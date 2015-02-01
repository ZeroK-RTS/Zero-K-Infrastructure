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

namespace LobbyClient
{

    public class OldNewPair<T>
    {
        public T Old;
        public T New;

        public OldNewPair(T old, T @new)
        {
            Old = old;
            New = @new;
        }
    }

    public class ChannelUserInfo
    {
        public Channel Channel;
        public List<User> Users;
    }

    public class ChannelUserRemovedInfo
    {
        public Channel Channel;
        public User User;
        public string Reason;
    }

    
    public class TasClient:Connection
    {
        public const int MaxAlliances = 16;
        public const int MaxTeams = 16;

        public ProtocolExtension Extensions { get; private set; }

        public delegate void Invoker();

        public delegate void Invoker<TArg>(TArg arg);


        readonly string appName = "UnknownClient";
        Dictionary<int, Battle> existingBattles = new Dictionary<int, Battle>();
        Dictionary<string, User> existingUsers = new Dictionary<string, User>();
        readonly bool forcedLocalIP = false;
        readonly Invoker<Invoker> guiThreadInvoker;
        bool isLoggedIn;
        Dictionary<string, Channel> joinedChannels = new Dictionary<string, Channel>();

        int lastUdpSourcePort;
        int lastUserStatus;
        readonly string localIp;
        readonly Timer minuteTimer;

        int pingInterval = 30; // how often to ping server (in seconds)
        readonly Timer pingTimer;
        public string serverHost { get; private set; }

        int serverPort;
        string serverVersion;

        public bool ConnectionFailed { get; private set; }

        public Dictionary<int, Battle> ExistingBattles { get { return existingBattles; } set { existingBattles = value; } }

        public Dictionary<string, User> ExistingUsers { get { return existingUsers; } set { existingUsers = value; } }

        
        public override async Task OnConnected()
        {
            MyBattle = null;
            ConnectionFailed = false;
            ExistingUsers = new Dictionary<string, User>();
            joinedChannels = new Dictionary<string, Channel>();
            existingBattles = new Dictionary<int, Battle>();
            isLoggedIn = false;
        }

        public override async Task OnConnectionClosed(bool wasRequested)
        {
            ConnectionFailed = !wasRequested;

            ExistingUsers = new Dictionary<string, User>();
            joinedChannels = new Dictionary<string, Channel>();
            existingBattles = new Dictionary<int, Battle>();
            MyBattle = null;
            isLoggedIn = false;
            ConnectionLost(this, new TasEventArgs(string.Format("Connection {0}", wasRequested ? "closed on user request" : "disconnected")));
        }

        public override async Task OnLineReceived(string line)
        {
            try {
                dynamic obj = CommandJsonSerializer.DeserializeLine(line);
                Input(this, line);
                await Process(obj);
            } catch (Exception ex) {
                Trace.TraceError("Error processing line {0} : {1}", line, ex);
            }
        }

        public async Task SendCommand<T>(T data)
        {
            try {
                var line = CommandJsonSerializer.SerializeToLine(data);
                Output(this, line.TrimEnd('\n'));
                await SendData(Encoding.GetBytes(line));
            } catch (Exception ex) {
                Trace.TraceError("Error sending {0} : {1}", data,ex);
            }
        }

        static CommandJsonSerializer CommandJsonSerializer = new CommandJsonSerializer();

        public bool IsLoggedIn { get { return isLoggedIn; } }

        public Dictionary<string, Channel> JoinedChannels { get { return joinedChannels; } }

        public Battle MyBattle { get; protected set; }
        //public int MyBattleID { get { return MyBattle != null ? MyBattle.BattleID : 0; } }


        public UserBattleStatus MyBattleStatus
        {
            get
            {
                if (MyBattle != null) {
                    UserBattleStatus ubs;
                    MyBattle.Users.TryGetValue(UserName, out ubs);
                    return ubs;
                }
                return null;
            }
        }

        public int MyBattleID
        {
            get
            {
                var bat = MyBattle;
                if (bat != null) return bat.BattleID;
                else return 0;
            }
        }

        public User MyUser
        {
            get
            {
                User us;
                ExistingUsers.TryGetValue(UserName, out us);
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

        public string UserName { get; private set; }
        public string UserPassword { get; private set; }

        public event EventHandler<User> UserAdded = delegate { };
        public event EventHandler<UserDisconnected> UserRemoved = delegate { };
        public event EventHandler<OldNewPair<User>> UserStatusChanged = delegate { };
        public event EventHandler<Battle> BattleFound = delegate { };
        public event EventHandler<ChannelUserInfo> ChannelUserAdded = delegate { };
        public event EventHandler<ChannelUserRemovedInfo> ChannelUserRemoved = delegate { };
        public event EventHandler<Welcome> Connected = delegate { };
        public event EventHandler<JoinChannelResponse> ChannelJoinFailed = delegate { };
        public event EventHandler<TasEventArgs> LoginAccepted = delegate { };
        public event EventHandler<LoginResponse> LoginDenied = delegate { };
        public event EventHandler<TasSayEventArgs> Said = delegate { }; // this is fired when any kind of say message is recieved
        public event EventHandler<SayingEventArgs> Saying = delegate { }; // this client is trying to say somethign
        public event EventHandler<BattleUserEventArgs> BattleUserJoined = delegate { };
        public event EventHandler<BattleUserEventArgs> BattleUserLeft = delegate { };
        public event EventHandler<CancelEventArgs<Channel>> PreviewChannelJoined = delegate { };
        public event EventHandler<TasEventArgs> RegistrationAccepted = delegate { };
        public event EventHandler<RegisterResponse> RegistrationDenied = delegate { };
        public event EventHandler<Battle> BattleRemoved = delegate { }; // raised just after the battle is removed from the battle list
        public event EventHandler<Battle> MyBattleRemoved = delegate { }; // raised just after the battle is removed from the battle list
        public event EventHandler<Battle> BattleClosed = delegate { };
        public event EventHandler<Battle> BattleOpened = delegate { };
        public event EventHandler<Battle> BattleJoined = delegate { };
        public event EventHandler<string> Output = delegate { }; // outgoing command and arguments
        public event EventHandler<EventArgs> HourChime = delegate { };
        public event EventHandler<string> Input = delegate { };
        public event EventHandler<UserBattleStatus> BattleUserStatusChanged = delegate { };
        public event EventHandler<UserBattleStatus> BattleMyUserStatusChanged = delegate { };
        public event EventHandler<Channel> ChannelJoined = delegate { };
        public event EventHandler<Channel> ChannelLeft = delegate { };
        public event EventHandler<BotBattleStatus> BattleBotAdded = delegate { };
        public event EventHandler<BotBattleStatus> BattleBotUpdated = delegate { };
        public event EventHandler<BotBattleStatus> BattleBotRemoved = delegate { };
        

        
        
        
        public event EventHandler<TasEventArgs> BattleDetailsChanged = delegate { };
        public event EventHandler<BattleInfoEventArgs> BattleInfoChanged = delegate { };
        public event EventHandler<BattleInfoEventArgs> BattleMapChanged = delegate { };
        


        public event EventHandler<TasEventArgs> ChannelTopicChanged = delegate { };
        public event EventHandler<TasEventArgs> ConnectionLost = delegate { };
        
        public event EventHandler<TasEventArgs> MyBattleHostExited = delegate { };
        public event EventHandler<BattleInfoEventArgs> MyBattleMapChanged = delegate { };
        public event EventHandler<TasEventArgs> MyBattleStarted = delegate { };

        public event EventHandler<CancelEventArgs<TasSayEventArgs>> PreviewSaid = delegate { };
        public event EventHandler<EventArgs<string>> Rang = delegate { };
        public event EventHandler<TasEventArgs> StartRectAdded = delegate { };
        public event EventHandler<TasEventArgs> StartRectRemoved = delegate { };


        public event EventHandler<EventArgs<User>> UserExtensionsChanged = delegate { };
        public event EventHandler<EventArgs<User>> MyExtensionsChanged = delegate { };
        
 

        public TasClient(Invoker<Invoker> guiThreadInvoker, string appName, Login.ClientTypes? clientTypes = null, string ipOverride = null)
        {
            if (clientTypes.HasValue) this.clientType = clientTypes.Value;
            this.appName = appName;
            this.guiThreadInvoker = guiThreadInvoker;


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

            Extensions = new ProtocolExtension(this, (user, data) => {
                                                                         User u;
                                                                         if (ExistingUsers.TryGetValue(user, out u))
                                                                         {
                                                                             UserExtensionsChanged(this, new EventArgs<User>(u));
                                                                         }


            });

            pingTimer = new Timer(pingInterval*1000) { AutoReset = true };
            pingTimer.Elapsed += OnPingTimer;
            pingTimer.Start();

            minuteTimer = new Timer(60000) { AutoReset = true };
            minuteTimer.Elapsed += (s, e) =>
                {
                    if (DateTime.Now.Minute == 0)
                        if (guiThreadInvoker != null) guiThreadInvoker(() => HourChime(this, new EventArgs()));
                        else HourChime(this, new EventArgs());
                };
            minuteTimer.Start();

        }


        public void AddBattleRectangle(int allyno, BattleRect rect)
        {
            {
                if (allyno < Spring.MaxAllies && allyno >= 0)
                {
                    RemoveBattleRectangle(allyno);
                    MyBattle.Rectangles.Add(allyno, rect);
//                    con.SendCommand("ADDSTARTRECT", allyno, rect.Left, rect.Top, rect.Right, rect.Bottom);
                }
            }
        }

        public Task AddBot(string name, string aiDll, int? allyNumber= null, int? teamNumber= null)
        {
            var u = new UpdateBotStatus();
            if (aiDll != null) u.AiLib = aiDll;
            if (name != null) u.Name = name;
            if (allyNumber != null) u.AllyNumber = allyNumber;
            if (teamNumber != null) u.TeamNumber = teamNumber;
            return SendCommand(u);
        }


        public void ChangeMap(string name)
        {
            // todo imple,emtn
            //{
//                mapToChangeTo = name;
  //              UpdateBattleInfo(lockToChangeTo, name);
    //        }
        }

        public async Task ChangeMyBattleStatus(bool? spectate = null,
                                         SyncStatuses? syncStatus = null,
                                         int? ally = null,
                                         int? team = null)
        {
            var ubs = MyBattleStatus;
            if (ubs != null) {
                var status = new UpdateUserBattleStatus() { IsSpectator = spectate, Sync = syncStatus, AllyNumber = ally, TeamNumber = team, Name = UserName};
                await SendCommand(status);
            }
        }


        public void ChangeMyUserStatus(bool? isAway = null, bool? isInGame = null)
        {
            User u;
            if (MyUser != null) u = MyUser.Clone();
            else u = new User();
            if (isAway != null) u.IsAway = isAway.Value;
            if (isInGame != null) u.IsInGame = isInGame.Value;
//            if (MyUser == null || lastUserStatus != u.ToInt())
  //          {
                //con.SendCommand("MYSTATUS", u.ToInt());
      //          lastUserStatus = u.ToInt();
    //        }
        }


        public void Connect(string host, int port)
        {
            serverHost = host;
            serverPort = port;
            Connect(host, port, forcedLocalIP ? localIp : null);
        }

        public static DateTime ConvertMilisecondTime(string arg)
        {
            return (new DateTime(1970, 1, 1, 0, 0, 0)).AddMilliseconds(double.Parse(arg, System.Globalization.CultureInfo.InvariantCulture));
        }


        public void RequestDisconnect()
        {
            RequestClose();
        }



        public async Task ForceAlly(string username, int ally)
        {
            if (MyBattle != null && MyBattle.Users.ContainsKey(username)) {
                var ubs = new UpdateUserBattleStatus() { Name = username, AllyNumber = ally };
                await SendCommand(ubs);
            }
        }

        public async Task ForceSpectator(string username, bool spectatorState = true)
        {
            if (MyBattle != null && MyBattle.Users.ContainsKey(username))
            {
                var ubs = new UpdateUserBattleStatus() { Name = username, IsSpectator = spectatorState };
                await SendCommand(ubs);
            }
        }

        public async Task ForceTeam(string username, int team)
        {
            if (MyBattle != null && MyBattle.Users.ContainsKey(username))
            {
                var ubs = new UpdateUserBattleStatus() { Name = username, TeamNumber = team };
                await SendCommand(ubs);
            }
        }

        public void GameSaid(string username, string text)
        {
            InvokeSaid(new TasSayEventArgs(SayPlace.Game, "", username, text, false));
        }


        public bool GetExistingUser(string name, out User u)
        {
            return ExistingUsers.TryGetValue(name, out u);
        }

        public User GetUserCaseInsensitive(string userName)
        {
            return ExistingUsers.Values.FirstOrDefault(u => String.Equals(u.Name, userName, StringComparison.InvariantCultureIgnoreCase));
        }


        public Task JoinBattle(int battleID, string password = null)
        {
            return SendCommand(new JoinBattle() { BattleID = battleID, Password = password });
        }


        public Task JoinChannel(string channelName, string key=null)
        {
            return SendCommand(new JoinChannel() { Name = channelName, Password = key });
        }

        public void Kick(string username)
        {
            //con.SendCommand("KICKFROMBATTLE", username);
        }

        public void AdminKickFromLobby(string username,string reason)
        {
            //con.SendCommand("KICKUSER", username,reason);
        }

        public void AdminSetTopic(string channel, string topic) {
            Say(SayPlace.User, "ChanServ", string.Format("!topic #{0} {1}", channel, topic.Replace("\n","\\n")),false);
        }

        public void AdminSetChannelPassword(string channel, string password) {
            if (string.IsNullOrEmpty(password)) {
                Say(SayPlace.User, "ChanServ",string.Format("!lock #{0} {1}", channel, password),false);    
            } else {
                Say(SayPlace.User, "ChanServ", string.Format("!unlock #{0}", channel), false);
            }
        }


        public async Task LeaveBattle()
        {
            if (MyBattle != null) {
                await SendCommand(new LeaveBattle() { BattleID = MyBattle.BattleID });
            }
        }


        public async Task LeaveChannel(string channelName)
        {
            if (joinedChannels.ContainsKey(channelName)) {
                await SendCommand(new LeaveChannel() { Name = channelName });
            }
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


        public Task Login(string userName, string password)
        {
            UserName = userName;
            UserPassword = password;
            return SendCommand(new Login() { Name = userName, PasswordHash = Utils.HashLobbyPassword(password), ClientType = clientType, UserID = GetMyUserID()});
        }


        Login.ClientTypes clientType = LobbyClient.Login.ClientTypes.ZeroKLobby | (Environment.OSVersion.Platform == PlatformID.Unix ? LobbyClient.Login.ClientTypes.Linux : 0);

        public Task OpenBattle(Battle nbattle)
        {
            LeaveBattle(); // leave current battle

            MyBattle = nbattle;

            MyBattle.Founder = ExistingUsers[UserName];
            MyBattle.Ip = localIp;

            //battle.Details.AddToParamList(objList);

            //con.SendCommand("OPENBATTLE", objList.ToArray());


            // send predefined starting rectangles
            //foreach (var v in MyBattle.Rectangles) con.SendCommand("ADDSTARTRECT", v.Key, v.Value.Left, v.Value.Top, v.Value.Right, v.Value.Bottom);


            return SendCommand(new OpenBattle() {
                Header =
                    new BattleHeader() {
                        Engine = nbattle.EngineVersion,
                        Game = nbattle.ModName,
                        Ip = nbattle.Ip,
                        Port = nbattle.HostPort,
                        Map = nbattle.MapName,
                        Password = nbattle.Password,
                        MaxPlayers = nbattle.MaxPlayers,
                        Title = nbattle.Title
                    }
            });
        }

      




        public Task Register(string username, string password)
        {
            return SendCommand(new Register() { Name = username, PasswordHash = Utils.HashLobbyPassword(password) });
        }

        public void RemoveBattleRectangle(int allyno)
        {
            if (MyBattle.Rectangles.ContainsKey(allyno))
            {
                MyBattle.Rectangles.Remove(allyno);
                //con.SendCommand("REMOVESTARTRECT", allyno);
            }
        }

        public async Task RemoveBot(string name)
        {
            var bat = MyBattle;
            if (bat != null && bat.Bots.ContainsKey(name)) await SendCommand(new RemoveBot{Name = name});
        }



        public void Ring(string username)
        {
            //con.SendCommand("RING", username);
        }


        public void ForceJoinBattle(string name, string battleHostName, string password = null) {
            var battle = ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == battleHostName);
            if (battle != null) ForceJoinBattle(name, battle.BattleID, password);
        }

        public void ForceJoinBattle(string name, int battleID, string password = null) {
            User user;
            Battle battle;
            existingUsers.TryGetValue(name, out user);
            existingBattles.TryGetValue(battleID, out battle);
            //if (user != null && battle != null) con.SendCommand("FORCEJOINBATTLE", name, battleID, password);
        }

        public void ForceJoinChannel(string user, string channel, string password= null) {
//            con.SendCommand(string.Format("FORCEJOIN {0} {1} {2}", user, channel, password));
        }

        public void ForceLeaveChannel(string user, string channel, string reason = null)
        {
//            con.SendCommand(string.Format("FORCELEAVECHANNEL {0} {1} {2}", channel, user, reason));
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

                if (args.SayPlace == SayPlace.Channel && !JoinedChannels.ContainsKey(args.Channel)) {
                    await JoinChannel(args.Channel);
                }

                var say = new Say() { Target = args.Channel, Place = args.SayPlace, Text = args.Text, IsEmote = args.IsEmote };

                await SendCommand(say);
            }
        }



        public Task SendRaw(string text)
        {
            if (!text.EndsWith("\n")) text += "\n";
            return SendData(Encoding.GetBytes(text));
        }

        public void SetHideCountry(string name, bool state) {
            //con.SendCommand("HIDECOUNTRYSET", name, state ? "1" : "0");
        }

        public void SetScriptTag(string data)
        {
            //con.SendCommand("SETSCRIPTTAGS", data);
        }

     
        /// <summary>
        /// Starts game and automatically does hole punching if necessary
        /// </summary>
        public void StartGame()
        {
            ChangeMyUserStatus(false, true);
        }

        public async Task UpdateBot(string name, string aiDll, int? allyNumber = null, int? teamNumber = null)
        {
            var bat = MyBattle;
            if (bat != null && bat.Bots.ContainsKey(name)) {
                await AddBot(name, aiDll, allyNumber, teamNumber);
            }
        }


        public async Task Process(UpdateBotStatus status)
        {
            var bat = MyBattle;
            if (bat != null)
            {
                BotBattleStatus ubs;
                bat.Bots.TryGetValue(status.Name, out ubs);
                if (ubs != null) {
                    ubs.UpdateWith(status);
                    BattleBotUpdated(this, ubs);
                } else {
                    var nubs = new BotBattleStatus(status.Name, status.Owner, status.AiLib);
                    nubs.UpdateWith(status);
                    bat.Bots[status.Name] = nubs;
                    BattleBotAdded(this, nubs);
                }
            }
        }

        public async Task Process(RemoveBot status)
        {
            var bat = MyBattle;
            if (bat != null)
            {
                BotBattleStatus ubs;
                if (bat.Bots.TryRemove(status.Name, out ubs)) {
                    BattleBotRemoved(this, ubs);
                }
            }
        }

       
        /// <summary>
        /// Primary method - processes commands from server
        /// </summary>
        /// <param Name="command">command Name</param>
        /// <param Name="args">command arguments</param>
        async Task ProcessCommand(object obj)
        {
            string command = "";
            string[] args = new string[]{};

            // is this really needed for the whole thread? it screws with date formatting
            // Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
               
                switch (command)
                {

                    case "RING":
                        Rang(this, new EventArgs<string>(args[0]));
                        break;


                    case "REDIRECT": // server sends backup IP
                        OnRedirect(args);
                        break;

                    case "CLIENTSTATUS": // client's status changed
                        OnClientStatus(args);
                        break;


                    case "CHANNELTOPIC": // channel topic update (after joining a channel)
                        OnChannelTopic(args);
                        break;


                    case "UPDATEBATTLEINFO": // update external battle info (lock and map)
                        OnUpdateBattleInfo(args);
                        break;


                    case "SETSCRIPTTAGS": // updates internal battle details
                        OnSetScriptTags(args);
                        break;

                    case "ADDSTARTRECT":
                        OnAddStartRect(args);
                        break;

                    case "REMOVESTARTRECT":
                        OnRemoveStartRect(args);
                        break;
                }
            }
            catch (Exception e)
            {
                //not throwing "ApplicationException" because we can explicitly say its application fault here:
                Trace.TraceError("TASC error: Error was thrown while processing chat command {0} \"{1}\" (check if chat event trigger faulty code in application): {2}", command, Utils.Glue(args), e);
            }
        }

        void OnRemoveStartRect(string[] args)
        {
            var allyNo = int.Parse(args[0]);
            MyBattle.Rectangles.Remove(allyNo);
            StartRectRemoved(this, new TasEventArgs(args));
        }

        void OnAddStartRect(string[] args)
        {
            var allyNo = int.Parse(args[0]);
            var left = int.Parse(args[1]);
            var top = int.Parse(args[2]);
            var right = int.Parse(args[3]);
            var bottom = int.Parse(args[4]);
            var rect = new BattleRect(left, top, right, bottom);
            MyBattle.Rectangles[allyNo] = rect;
            StartRectAdded(this, new TasEventArgs(args));
        }

        void OnSetScriptTags(string[] args)
        {
            //var bd = new BattleDetails();
            //bd.Parse(Utils.Glue(args), MyBattle.ModOptions);
            //MyBattle.Details = bd;
            MyBattle.ScriptTags.AddRange(args);
            BattleDetailsChanged(this, new TasEventArgs(args));
        }



        void OnUpdateBattleInfo(string[] args)
        {
            var battleID = Int32.Parse(args[0]);
            var specCount = Int32.Parse(args[1]);
            var mapName = Utils.Glue(args, 4);
            var mapHash = Int32.Parse(args[3]);
            var isLocked = Int32.Parse(args[2]) > 0;

            Battle battle;
            if (!existingBattles.TryGetValue(battleID, out battle)) return;
            battle.SpectatorCount = specCount;

            var bi = new BattleInfoEventArgs(battleID, specCount, mapName, mapHash, isLocked);

            if (battle.MapName != mapName) {
                battle.MapName = mapName;
                if (battle == MyBattle) MyBattleMapChanged(this, bi);
                BattleMapChanged(this, bi);
            }

            BattleInfoChanged(this, bi);
        }



        void OnChannelTopic(string[] args)
        {
            var c = JoinedChannels[args[0]];
            c.TopicSetBy = args[1];
            c.TopicSetDate = ConvertMilisecondTime(args[2]);
            c.Topic = Utils.Glue(args, 3);
            ChannelTopicChanged(this, new TasEventArgs(args[0]));
        }

        void OnClientStatus(string[] args)
        {
            int status;
            int.TryParse(args[1], out status);

            var u = ExistingUsers[args[0]];
            var old = u.Clone();
            //u.FromInt(status);

            //if (u.Name == UserName) lastUserStatus = u.ToInt();
            if (MyBattle != null && MyBattle.Founder.Name == u.Name) {
                if (u.IsInGame && old.IsInGame == false) MyBattleStarted(this, new TasEventArgs(args));
                if (!u.IsInGame && old.IsInGame == true) MyBattleHostExited(this, new TasEventArgs(args));
            }

            //UserStatusChanged(this, new TasEventArgs(args));
        }

        void OnRedirect(string[] args)
        {
            var host = args[0];
            var port = int.Parse(args[1]);
            Connect(host, port);
        }



        async Task Process(BattleAdded bat)
        {
            var newBattle = new Battle();
            newBattle.UpdateWith(bat.Header,existingUsers);
            existingBattles[newBattle.BattleID] = newBattle;
            newBattle.Founder.IsInBattleRoom = true;
            
            BattleFound(this, newBattle);
        }

        async Task Process(JoinedBattle bat)
        {
            User user;
            existingUsers.TryGetValue(bat.User, out user);
            Battle battle;
            ExistingBattles.TryGetValue(bat.BattleID, out battle);
            if (user != null && battle != null) {
                battle.Users[user.Name] = new UserBattleStatus(user.Name, user);
                user.IsInBattleRoom = true;
                BattleUserJoined(this, new BattleUserEventArgs(user.Name, bat.BattleID));
                if (user.Name == UserName) {
                    MyBattle = battle;
                    if (battle.Founder.Name == UserName) BattleOpened(this, battle);
                    BattleJoined(this, MyBattle);
                }
            }
        }


        async Task Process(LeftBattle left)
        {
            var user = existingUsers[left.User];
            var bat = ExistingBattles[left.BattleID];

            if (bat != null) {
                user.IsInBattleRoom = false;
                UserBattleStatus removed;
                bat.Users.TryRemove(left.User, out removed);

                if (MyBattle != null && left.BattleID == MyBattleID) {
                    if (UserName == left.User) {
                        bat.ScriptTags.Clear();
                        bat.Bots.Clear();

                        MyBattle = null;
                        BattleClosed(this, bat);
                    }
                }
                BattleUserLeft(this, new BattleUserEventArgs(user.Name, left.BattleID));
            }
        }

        async Task Process(BattleRemoved br)
        {
            Battle battle;
            if (!existingBattles.TryGetValue(br.BattleID, out battle)) return;
            foreach (var u in battle.Users.Keys)
            {
                User user;
                if (ExistingUsers.TryGetValue(u, out user)) user.IsInBattleRoom = false;
            }
            if (battle == MyBattle)
            {
                battle.ScriptTags.Clear();
                battle.Users.Clear();
                BattleClosed(this, battle);
                MyBattleRemoved(this, battle);
            }
            existingBattles.Remove(br.BattleID);
            BattleRemoved(this, battle);
        }


        async Task Process(LoginResponse loginResponse)
        {
            if (loginResponse.ResultCode == LoginResponse.Code.Ok) {
                isLoggedIn = true;
                LoginAccepted(this, new TasEventArgs());
            } else {
                isLoggedIn = false;
                LoginDenied(this, loginResponse);
            }
        }

        async Task Process(User user)
        {
            User old;
            existingUsers.TryGetValue(user.Name, out old);
            existingUsers[user.Name] = user;
            if (old == null) UserAdded(this, user);
            UserStatusChanged(this, new OldNewPair<User>(old, user));
        }


        async Task Process(RegisterResponse registerResponse)
        {
            if (registerResponse.ResultCode == RegisterResponse.Code.Ok)
            {
                RegistrationAccepted(this, new TasEventArgs());
            }
            else
            {
                RegistrationDenied(this, registerResponse);
            }
        }

        async Task Process(Say say)
        {
            InvokeSaid(new TasSayEventArgs(say.Place, say.Target,say.User, say.Text, say.IsEmote));
        }



        public Welcome ServerWelcome = new Welcome();


        async Task Process(Welcome welcome)
        {
            ServerWelcome = welcome;
            Connected(this, welcome);
        }

        async Task Process(JoinChannelResponse response)
        {
            if (response.Success) {
                var chan = new Channel() {
                    Name = response.Channel.Name,
                    Topic = response.Channel.Topic,
                    TopicSetBy = response.Channel.TopicSetBy,
                    TopicSetDate = response.Channel.TopicSetDate,
                };
                
                JoinedChannels[response.Name] = chan;

                foreach (var u in response.Channel.Users) {
                    User user;
                    if (existingUsers.TryGetValue(u, out user)) chan.Users[u] = user;
                }
                
                var cancelEvent = new CancelEventArgs<Channel>(chan);
                PreviewChannelJoined(this, cancelEvent);
                if (!cancelEvent.Cancel) {
                    ChannelJoined(this, chan);
                    ChannelUserAdded(this, new ChannelUserInfo() {Channel = chan, Users = chan.Users.Values.ToList()}); 
                }
            } else {
                ChannelJoinFailed(this, response);
            }
        }

        async Task Process(ChannelUserAdded arg)
        {
            Channel chan;
            if (joinedChannels.TryGetValue(arg.ChannelName, out chan)) {
                if (!chan.Users.ContainsKey(arg.UserName)) {
                    User user;
                    if (existingUsers.TryGetValue(arg.UserName, out user)) {
                        chan.Users[arg.UserName] = user;
                        ChannelUserAdded(this, new ChannelUserInfo() { Channel = chan, Users = new List<User>(){user}});
                    }
                }
            }
        }

        async Task Process(ChannelUserRemoved arg)
        {
            Channel chan;
            if (joinedChannels.TryGetValue(arg.ChannelName, out chan)) {
                User org;
                if (chan.Users.TryRemove(arg.UserName, out org))
                {
                    if (arg.UserName == UserName) ChannelLeft(this, chan);
                    ChannelUserRemoved(this, new ChannelUserRemovedInfo() { Channel = chan, User = org });
                }
            }
        }

        async Task Process(UserDisconnected arg)
        {
            UserRemoved(this, arg);
            existingUsers.Remove(arg.Name);
        }

        async Task Process(UpdateUserBattleStatus status)
        {
            var bat = MyBattle;
            if (bat != null) {
                UserBattleStatus ubs;
                bat.Users.TryGetValue(status.Name, out ubs);
                if (ubs != null) {
                    ubs.UpdateWith(status);
                    if (status.Name == UserName) BattleMyUserStatusChanged(this, ubs);
                    BattleUserStatusChanged(this, ubs);
                }
            }
        }


        void InvokeSaid(TasSayEventArgs sayArgs)
        {
            var previewSaidEventArgs = new CancelEventArgs<TasSayEventArgs>(sayArgs);
            PreviewSaid(this, previewSaidEventArgs);
            if (!previewSaidEventArgs.Cancel) Said(this, sayArgs);
        }



        void OnPingTimer(object sender, EventArgs args)
        {
/*            if (con != null && IsConnected) {
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
            }*/
        }


    }
}