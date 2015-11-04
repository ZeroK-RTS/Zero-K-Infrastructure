#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using ZkData;

#endregion

namespace LobbyClient.Legacy
{
    public class TasClient
    {
        public delegate void Invoker();

        public delegate void Invoker<TArg>(TArg arg);

        public enum SayPlace
        {
            Channel,
            Battle,
            User,
            BattlePrivate
        };

        StringBuilder agreementText;

        readonly string appName = "UnknownClient";
        ServerConnection con;
        Dictionary<string, ExistingChannel> existingChannels = new Dictionary<string, ExistingChannel>();
        Dictionary<string, User> existingUsers = new Dictionary<string, User>();
        readonly bool forcedLocalIP = false;
        readonly Invoker<Invoker> guiThreadInvoker;
        bool isChanScanning;
        bool isLoggedIn;
        Dictionary<string, Channel> joinedChannels = new Dictionary<string, Channel>();
        int lastSpectatorCount;
        int lastUdpSourcePort;
        int lastUserBattleStatus;
        int lastUserStatus;
        readonly string localIp;
        bool lockToChangeTo;
        int mapChecksumToChangeTo;
        string mapToChangeTo;

        int pingInterval = 30; // how often to ping server (in seconds)
        readonly Timer pingTimer;
        public string serverHost { get; private set; }

        int serverPort;

        // user info 
        string username = "";
        public bool ConnectionFailed { get; private set; }

        public Dictionary<string, ExistingChannel> ExistingChannels { get { return existingChannels; } }

        public Dictionary<string, User> ExistingUsers { get { return existingUsers; } set { existingUsers = value; } }

        public bool IsConnected { get { return con != null && con.IsConnected; } }
        public bool IsLoggedIn { get { return isLoggedIn; } }
        public Dictionary<string, Channel> JoinedChannels { get { return joinedChannels; } }
        public int MessageID { get; private set; }

        public User MyUser
        {
            get
            {
                User us;
                ExistingUsers.TryGetValue(username, out us);
                return us;
            }
        }


        public int PingInterval
        {
            get { return pingInterval; }
            set
            {
                pingInterval = value;
                pingTimer.Interval = pingInterval*1000;
            }
        }
        public string ServerSpringVersion { get; private set; }

        public string UserName { get { return username; } }
        public string UserPassword;

        public event EventHandler<TasEventAgreementRecieved> AgreementRecieved = delegate { };
        public event EventHandler<TasEventArgs> ChannelForceLeave = delegate { }; // i was kicked from a channel
        public event EventHandler<TasEventArgs> ChannelJoinFailed = delegate { };
        public event EventHandler<TasEventArgs> ChannelJoined = delegate { };
        public event EventHandler<CancelEventArgs<string>> ChannelLeaving = delegate { }; // raised before attempting to leave a channel
        public event EventHandler<TasEventArgs> ChannelLeft = delegate { };
        public event EventHandler<TasEventArgs> ChannelListDone = delegate { };
        public event EventHandler<TasEventArgs> ChannelUserAdded = delegate { };
        public event EventHandler<TasEventArgs> ChannelUserRemoved = delegate { };
        public event EventHandler<TasEventArgs> ChannelUsersAdded = delegate { }; // raised after a group of clients is recieved in a batch
        public event EventHandler<TasEventArgs> Connected = delegate { };
        public event EventHandler<TasEventArgs> ConnectionLost = delegate { };
        public event EventHandler<TasEventArgs> Failure = delegate { }; //this event is fired whenever any failure events fire
        public event EventHandler<TasInputArgs> Input = delegate { };
        public event EventHandler<KickedFromServerEventArgs> KickedFromServer = delegate { };
        public event EventHandler<TasEventArgs> LoginAccepted = delegate { };
        public event EventHandler<TasEventArgs> LoginDenied = delegate { };
        public event EventHandler<EventArgs<KeyValuePair<string, object[]>>> Output = delegate { }; // outgoing command and arguments
        public event EventHandler<CancelEventArgs<TasEventArgs>> PreviewChannelJoined = delegate { };
        public event EventHandler<CancelEventArgs<TasSayEventArgs>> PreviewSaid = delegate { };
        public event EventHandler<TasEventArgs> RegistrationAccepted = delegate { };
        public event EventHandler<TasEventArgs> RegistrationDenied = delegate { };
        public event EventHandler<TasSayEventArgs> Said = delegate { }; // this is fired when any kind of say message is recieved
        public event EventHandler<SayingEventArgs> Saying = delegate { }; // this client is trying to say somethign
        public event EventHandler<EventArgs<User>> UserAdded = delegate { };
        public event EventHandler<TasEventArgs> UserRemoved = delegate { };
        public event EventHandler<TasEventArgs> UserStatusChanged = delegate { };
 

        public TasClient(Invoker<Invoker> guiThreadInvoker, string appName, int cpu, bool invokeUserStatusChangedOnExtensions = false, string ipOverride = null)
        {
            this.cpu = cpu;
            this.appName = appName;
            this.guiThreadInvoker = guiThreadInvoker;

            con = new ServerConnection();
            con.ConnectionClosed += OnConnectionClosed;
            con.CommandRecieved += OnCommandRecieved;
            con.CommandSent += (s, e) => Output(this, e);

            if (!string.IsNullOrEmpty(ipOverride))
            {
                localIp = ipOverride;
                forcedLocalIP = true;
            }
            else
            {
                var addresses = Dns.GetHostAddresses(Dns.GetHostName());

                localIp = addresses[0].ToString();
                foreach (var adr in addresses)
                {
                    if (adr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIp = adr.ToString();
                        break;
                    }
                }
            }

            pingTimer = new Timer(pingInterval*1000) { AutoReset = true };
            pingTimer.Elapsed += OnPingTimer;
            pingTimer.Start();

            ConnectionLost += RaiseFailure;
            LoginDenied += RaiseFailure;
            ChannelJoinFailed += RaiseFailure;
        }

        public void AcceptAgreement()
        {
            con.SendCommand("CONFIRMAGREEMENT");
        }




        public void ChangeMyUserStatus(bool? isAway = null, bool? isInGame = null)
        {
            User u;
            if (MyUser != null) u = MyUser.Clone();
            else u = new User();
            u.FromInt(lastUserStatus);
            if (isAway != null) u.IsAway = isAway.Value;
            if (isInGame != null) u.IsInGame = isInGame.Value;
            if (MyUser == null || lastUserStatus != u.ToInt())
            {
                con.SendCommand("MYSTATUS", u.ToInt());
                lastUserStatus = u.ToInt();
            }
        }


        public void Connect(string host, int port)
        {
            serverHost = host;
            serverPort = port;
            ConnectionFailed = false;
            ExistingUsers = new Dictionary<string, User>();
            existingChannels = new Dictionary<string, ExistingChannel>();
            joinedChannels = new Dictionary<string, Channel>();
            isChanScanning = false;
            isLoggedIn = false;
            username = "";
            if (con.IsConnected) con.RequestClose();
            con.Connect(host, port, forcedLocalIP ? localIp : null);
        }

        public void RequestDisconnect()
        {
            con.RequestClose();
        }

        void OnDisconnected()
        {
            ExistingUsers = new Dictionary<string, User>();
            existingChannels = new Dictionary<string, ExistingChannel>();
            joinedChannels = new Dictionary<string, Channel>();
            username = "";
            isLoggedIn = false;
            isChanScanning = false;
            if (guiThreadInvoker != null) guiThreadInvoker(() => ConnectionLost(this, new TasEventArgs("Connection was closed")));
            else ConnectionLost(this, new TasEventArgs("Connection was closed"));
        }


        public Dictionary<string, ExistingChannel> GetExistingChannels()
        {
            if (isChanScanning) throw new TasClientException("Channel scan operation in progress");
            return new Dictionary<string, ExistingChannel>(ExistingChannels);
        }

        public bool GetExistingUser(string name, out User u)
        {
            return ExistingUsers.TryGetValue(name, out u);
        }

        public User GetUserCaseInsensitive(string userName)
        {
            return ExistingUsers.Values.FirstOrDefault(u => String.Equals(u.Name, userName, StringComparison.InvariantCultureIgnoreCase));
        }


        public void JoinChannel(string channelName, string key=null)
        {
            if (con == null) 
            {
                System.Diagnostics.Trace.TraceError("ERROR TasClient/JoinChannel: No server connection yet");
                return;
            }
            if (!String.IsNullOrEmpty(key)) con.SendCommand("JOIN", channelName, key);
            else con.SendCommand("JOIN", channelName);
        }

        public void LeaveChannel(string channelName)
        {
            var args = new CancelEventArgs<string>(channelName);
            ChannelLeaving(this, args);
            if (args.Cancel) return;
            con.SendCommand("LEAVE", channelName);
            JoinedChannels.Remove(channelName);
            ChannelLeft(this, new TasEventArgs(channelName));
        }

        public void ListChannels()
        {
            isChanScanning = true;
            ExistingChannels.Clear();
            con.SendCommand("CHANNELS");
        }

        public static string GetMyUserID() {
            var nics = NetworkInterface.GetAllNetworkInterfaces().Where(x=> !String.IsNullOrWhiteSpace(x.GetPhysicalAddress().ToString())
                && x.NetworkInterfaceType != NetworkInterfaceType.Loopback && x.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

            var wantedNic = nics.FirstOrDefault();

            if (wantedNic != null)
            {
                return Crc.Crc32(wantedNic.GetPhysicalAddress().GetAddressBytes()).ToString();
            }
            return "0";
        }


        public void Login(string userName, string password)
        {
            if (con == null) throw new TasClientException("Not connected");

            UserPassword = password;

            con.SendCommand("LOGIN", userName, Utils.HashLobbyPassword(password), cpu, localIp, appName, "\t" + GetMyUserID(), "\ta sp m cl p");
        }


        int cpu = Environment.OSVersion.Platform == PlatformID.Unix ? GlobalConst.ZkLobbyUserCpuLinux : GlobalConst.ZkLobbyUserCpu;


        public void Register(string username, string password)
        {
            con.SendCommand("REGISTER", username, Utils.HashLobbyPassword(password));
        }



        /// <summary>
        /// Say something through chat system
        /// </summary>
        /// <param Name="place">Pick user (private message) channel or battle</param>
        /// <param Name="channel">Channel or User Name</param>
        /// <param Name="inputtext">chat text</param>
        /// <param Name="isEmote">is message emote? (channel or battle only)</param>
        /// <param Name="linePrefix">text to be inserted in front of each line (example: "!pm xyz")</param>
        public async Task Say(SayPlace place, string channel, string inputtext, bool isEmote, string linePrefix = "")
        {
            if (String.IsNullOrEmpty(inputtext)) return;
            var lines = inputtext.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var text in lines)
            {
                if (String.IsNullOrEmpty(text)) continue;

                var sentText = linePrefix + text;

                var args = new SayingEventArgs(place, channel, sentText, isEmote);
                Saying(this, args);
                if (args.Cancel) continue;

                switch (place)
                {
                    case SayPlace.Channel:
                        if (!JoinedChannels.ContainsKey(args.Channel)) JoinChannel(args.Channel);
                        if (args.IsEmote) await con.SendCommand("SAYEX", args.Channel, args.Text);
                        else await con.SendCommand("SAY", args.Channel, args.Text);
                        break;

                    case SayPlace.User:
                        await con.SendCommand("SAYPRIVATE", args.Channel, args.Text);
                        break;

                    case SayPlace.Battle:
                        if (args.IsEmote) await con.SendCommand("SAYBATTLEEX", args.Text);
                        else await con.SendCommand("SAYBATTLE", args.Text);
                        break;
                    case SayPlace.BattlePrivate:
                        if (args.IsEmote) await con.SendCommand("SAYBATTLEPRIVATEEX", channel, args.Text);
                        else await con.SendCommand("SAYBATTLEPRIVATE", channel, args.Text);
                        break;
                }
            }
        }


        void DispatchServerCommand(string command, string[] args)
        {
            if (guiThreadInvoker != null) guiThreadInvoker(() => DispatchServerCommandOnGuiThread(command, args));
            else DispatchServerCommandOnGuiThread(command, args);
        }

        /// <summary>
        /// Primary method - processes commands from server
        /// </summary>
        /// <param Name="command">command Name</param>
        /// <param Name="args">command arguments</param>
        void DispatchServerCommandOnGuiThread(string command, string[] args)
        {
            // is this really needed for the whole thread? it screws with date formatting
            // Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                if (command.StartsWith("#"))
                {
                    MessageID = int.Parse(command.Substring(1));
                    command = args[0];
                    args = Utils.ShiftArray(args, -1);
                }

                Input(this, new TasInputArgs(command, args));

                switch (command)
                {
                    case "TASServer": // happens after connecting to server
                        OnTasServer(args);
                        break;

                    case "ACCEPTED": // Login accepted
                        OnAccepted(args);
                        break;

                    case "DENIED": // login denied
                        OnDenied(args);
                        break;

                    case "JOIN": // channel joined
                        OnJoin(args);
                        break;

                    case "JOINFAILED": // channel join failed
                        ChannelJoinFailed(this, new TasEventArgs(Utils.Glue(args)));
                        break;

                    case "CHANNEL": // iterating channels
                        OnChannel(args);
                        break;

                    case "ENDOFCHANNELS": // end of channel list iteration
                        OnEndOfChannels();
                        break;

                    case "ADDUSER": // new user joined ta server
                        OnAddUser(args);
                        break;

                    case "FORCELEAVECHANNEL":
                        OnForceLeaveChannel(args);
                        break;

                    case "KICKUSER":
                        OnKickUser(args);
                        break;

                    case "REMOVEUSER": // user left ta server
                        OnRemoveUser(args);
                        break;

                    case "MOTD": // server motd
                        OnMotd(args);
                        break;

                    case "SERVERMSG": // server message
                        OnServerMsg(args);
                        break;

                    case "SERVERMSGBOX": // server messagebox
                        InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Server,
                                                       TasSayEventArgs.Places.MessageBox,
                                                       "",
                                                       "",
                                                       Utils.Glue(args, 0),
                                                       false));
                        break;

                    case "CHANNELMESSAGE": // server broadcast to channel
                        InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Server,
                                                       TasSayEventArgs.Places.Channel,
                                                       args[0],
                                                       "",
                                                       Utils.Glue(args, 1),
                                                       false));
                        break;

                    case "SAID": // someone said something in channel
                        InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Player,
                                                       TasSayEventArgs.Places.Channel,
                                                       args[0],
                                                       args[1],
                                                       Utils.Glue(args, 2),
                                                       false));
                        break;


                    case "SAIDEX": // someone said something with emote in channel
                        InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Player,
                                                       TasSayEventArgs.Places.Channel,
                                                       args[0],
                                                       args[1],
                                                       Utils.Glue(args, 2),
                                                       true));
                        break;

                    case "SAYPRIVATE": // sent back from sever when user sends private message
                        InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Player,
                                                       TasSayEventArgs.Places.Normal,
                                                       args[0],
                                                       username,
                                                       Utils.Glue(args, 1),
                                                       false));
                        break;

                    case "SAIDPRIVATE": // someone said something to me
                        InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Player,
                                                       TasSayEventArgs.Places.Normal,
                                                       args[0],
                                                       args[0],
                                                       Utils.Glue(args, 1),
                                                       false));
                        break;

                    case "SAIDBATTLE": // someone said something in battle
                        InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Player,
                                                       TasSayEventArgs.Places.Battle,
                                                       "",
                                                       args[0],
                                                       Utils.Glue(args, 1),
                                                       false));
                        break;

                    case "SAIDBATTLEEX": // someone said in battle with emote

                        InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Player,
                                                       TasSayEventArgs.Places.Battle,
                                                       "",
                                                       args[0],
                                                       Utils.Glue(args, 1),
                                                       true));
                        break;
                    case "BROADCAST": // server sends urgent broadcast
                        InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Server,
                                                       TasSayEventArgs.Places.Broadcast,
                                                       "",
                                                       "",
                                                       Utils.Glue(args, 0),
                                                       false));
                        break;

                    case "REDIRECT": // server sends backup IP
                        OnRedirect(args);
                        break;

                    case "CLIENTSTATUS": // client's status changed
                        OnClientStatus(args);
                        break;

                    case "CLIENTS": // client list sent after channel join
                        OnClients(args);
                        break;

                    case "JOINED": // user joined one of my channels
                        OnJoined(args);
                        break;

                    case "LEFT": // user left one of my channels
                        OnLeft(args);
                        break;


                    
                    case "AGREEMENT":
                        OnAgreement(args);
                        break;

                    case "PONG":
                        lastPong = DateTime.UtcNow;
                        break;

                    case "AGREEMENTEND":
                        OnAgreementEnd();
                        break;

                    case "REGISTRATIONDENIED":
                        RegistrationDenied(this, new TasEventArgs(Utils.Glue(args)));
                        break;

                    case "REGISTRATIONACCEPTED":
                        RegistrationAccepted(this, new TasEventArgs(args));
                        break;

                }
            }
            catch (Exception e)
            {
                //not throwing "ApplicationException" because we can explicitly say its application fault here:
                Trace.TraceError("TASC error: Error was thrown while processing chat command {0} \"{1}\" (check if chat event trigger faulty code in application): {2}", command, Utils.Glue(args), e);
            }
        }

        void OnAgreementEnd()
        {
            AgreementRecieved(this, new TasEventAgreementRecieved(agreementText));
            agreementText = null;
        }

        void OnAgreement(string[] args)
        {
            if (agreementText == null) agreementText = new StringBuilder();
            agreementText.AppendLine(Utils.Glue(args));
        }

        void OnLeft(string[] args)
        {
            var channelName = args[0];
            var userName = args[1];
            var reason = Utils.Glue(args, 2);
            var channel = JoinedChannels[channelName];
            channel.ChannelUsers.Remove(userName);
            ChannelUserRemoved(this, new TasEventArgs(channelName, userName, reason));
        }

        void OnJoined(string[] args)
        {
            var channelName = args[0];
            var userName = args[1];
            var channel = JoinedChannels[channelName];
            channel.ChannelUsers.Add(userName);
            ChannelUserAdded(this, new TasEventArgs(channelName, userName));
        }

        void OnClients(string[] args)
        {
            var usrs = Utils.Glue(args, 1).Split(' ');
            foreach (var s in usrs) JoinedChannels[args[0]].ChannelUsers.Add(s);
            ChannelUsersAdded(this, new TasEventArgs(args));
        }

        void OnClientStatus(string[] args)
        {
            int status;
            int.TryParse(args[1], out status);

            if (!existingUsers.ContainsKey(args[0])) return;
            var u = ExistingUsers[args[0]];
            var old = u.Clone();
            u.FromInt(status);

            if (u.Name == username) lastUserStatus = u.ToInt();

            UserStatusChanged(this, new TasEventArgs(args));
        }

        void OnRedirect(string[] args)
        {
            var host = args[0];
            var port = int.Parse(args[1]);
            Connect(host, port);
        }

        void OnServerMsg(string[] args)
        {
            InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Server, TasSayEventArgs.Places.Server, "", "", Utils.Glue(args, 0), false));
        }

        void OnMotd(string[] args)
        {
            if (args.Length > 0) InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Server, TasSayEventArgs.Places.Motd, "", "", Utils.Glue(args, 0), false));
        }

        void OnRemoveUser(string[] args)
        {
            var userName = args[0];
            //var user = ExistingUsers[userName];   // unused; just gives KeyNotFound exceptions
            UserRemoved(this, new TasEventArgs(args));
            ExistingUsers.Remove(userName);
        }

        void OnKickUser(string[] args)
        {
            Trace.TraceInformation(String.Format("User {0} kicked (we are {1})"), args[1], MyUser.Name);
            string us = args[1];
            if (us == MyUser.Name) {
                string kicker = args[0];
                string reason = args.Length > 2 ? string.Join(" ", args, 2) : null;
                KickedFromServer(this, new KickedFromServerEventArgs(kicker, reason));
            }
        }

        void OnForceLeaveChannel(string[] args)
        {
            var channel = args[0];
            JoinedChannels.Remove(channel);
            ChannelForceLeave(this, new TasEventArgs(args));
        }

        void OnAddUser(string[] args)
        {
            try {
                var u = User.Create(args[0]);
                u.Country = args[1];
                int cpu;
                int.TryParse(args[2], out cpu);
                u.Cpu = cpu;
                u.LobbyID = Convert.ToInt32(args[3]);
                ExistingUsers.Add(u.Name, u);
                UserAdded(this, new EventArgs<User>(u));
            } catch (Exception e) {
                //TraceError ensure bright red coloured message (WriteLine looked harmless):
                Trace.TraceError("Error was thrown while processing chat command ADDUSER (check if this event trigger faulty code in application): " + e);
            }
        }

        void OnEndOfChannels()
        {
            isChanScanning = false;
            ChannelListDone(this, new TasEventArgs());
        }

        void OnChannel(string[] args)
        {
            var c = new ExistingChannel();
            c.name = args[0];
            int.TryParse(args[1], out c.userCount);
            if (args.Length >= 3) c.topic = Utils.Glue(args, 2);
            ExistingChannels.Add(c.name, c);
        }

        void OnJoin(string[] args)
        {
            if (!JoinedChannels.ContainsKey(args[0])) {
                JoinedChannels.Add(args[0], Channel.Create(args[0]));

                var cancelEventArgs = new CancelEventArgs<TasEventArgs>(new TasEventArgs(args));
                PreviewChannelJoined(this, cancelEventArgs);
                if (!cancelEventArgs.Cancel) ChannelJoined(this, new TasEventArgs(args));
            }
        }

        void OnDenied(string[] args)
        {
            isLoggedIn = false;
            var reason = Utils.Glue(args);
            LoginDenied(this, new TasEventArgs(reason));
        }

        void OnAccepted(string[] args)
        {
            username = args[0];
            isLoggedIn = true;
            if (LoginAccepted != null) LoginAccepted(this, new TasEventArgs());
        }

        void OnTasServer(string[] args)
        {
            serverVersion = args[0];
            ServerSpringVersion = args[1];
            Connected(this, new TasEventArgs());
        }

        void InvokeSaid(TasSayEventArgs sayArgs)
        {
            var previewSaidEventArgs = new CancelEventArgs<TasSayEventArgs>(sayArgs);
            PreviewSaid(this, previewSaidEventArgs);
            if (!previewSaidEventArgs.Cancel) Said(this, sayArgs);
        }


        void OnCommandRecieved(object sender, ConnectionEventArgs args)
        {
            if (sender == con) DispatchServerCommand(args.Command, args.Parameters);
        }

        void OnConnectionClosed(object sender, EventArgs args)
        {
            ConnectionFailed = true;
            OnDisconnected();
        }

        DateTime lastPing;
        DateTime lastPong;
        string serverVersion;

        void OnPingTimer(object sender, EventArgs args)
        {
            if (con != null && IsConnected) {
                //if (lastPing != DateTime.MinValue && lastPing.Subtract(lastPong).TotalSeconds > pingInterval*2) {
                    // server didnt respond to ping in 30-60s 
                   // con.RequestClose();
                //} else {
                    lastPing = DateTime.UtcNow;
                    con.SendCommand("PING");
                //}
            }
            else if (ConnectionFailed && !IsConnected)
            {
                Connect(serverHost, serverPort);
            }
        }

        /// <summary>
        /// purpose of this event handler is to redirect "fail" events to failure event too
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="args"></param>
        void RaiseFailure(object sender, TasEventArgs args)
        {
            Failure(this, args);
            Console.Out.Flush();
        }


        public void ChangePassword(string old, string newPass) {
            con.SendCommand("CHANGEPASSWORD", Utils.HashLobbyPassword(old), Utils.HashLobbyPassword(newPass));
        }
    }
}