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
using System.Threading;
using System.Threading.Tasks;
using PlasmaShared;
using ZkData;
using ZkData.UnitSyncLib;
using Timer = System.Timers.Timer;

#endregion

namespace LobbyClient
{
    
    public class TasClient
    {
        ITransport transport;

        public const int MaxAlliances = 16;
        public const int MaxTeams = 16;


        public delegate void Invoker();

        public delegate void Invoker<TArg>(TArg arg);

        public bool IsConnected
        {
            get { return transport != null && transport.IsConnected; }
        }


        readonly string appName = "UnknownClient";
        Dictionary<int, Battle> existingBattles = new Dictionary<int, Battle>();
        Dictionary<string, User> existingUsers = new Dictionary<string, User>();
        readonly bool forcedLocalIP = false;
        bool isLoggedIn;
        Dictionary<string, Channel> joinedChannels = new Dictionary<string, Channel>();

        int lastUdpSourcePort;
        int lastUserStatus;
        readonly string localIp;

        int pingInterval = 30; // how often to ping server (in seconds)
        readonly Timer pingTimer;
        public string serverHost { get; private set; }

        int serverPort;
        public Dictionary<int, Battle> ExistingBattles { get { return existingBattles; } set { existingBattles = value; } }

        public Dictionary<string, User> ExistingUsers { get { return existingUsers; } set { existingUsers = value; } }

        
        public async Task OnConnected()
        {
            MyBattle = null;
            ExistingUsers = new Dictionary<string, User>();
            joinedChannels = new Dictionary<string, Channel>();
            existingBattles = new Dictionary<int, Battle>();
            isLoggedIn = false;
        }

        public async Task OnConnectionClosed(bool wasRequested)
        {
            ExistingUsers = new Dictionary<string, User>();
            joinedChannels = new Dictionary<string, Channel>();
            existingBattles = new Dictionary<int, Battle>();

            MyBattle = null;
            isLoggedIn = false;
            ConnectionLost(this, new TasEventArgs(string.Format("Connection {0}", wasRequested ? "closed on user request" : "disconnected")));
        }

        public async Task OnCommandReceived(string line)
        {
            try {
                Input(this, line);
                dynamic obj = CommandJsonSerializer.DeserializeLine(line);
                await Process(obj);
            } catch (Exception ex) {
                Trace.TraceError("Error processing line {0} : {1}", line, ex);
            }
        }

        public async Task SendCommand<T>(T data)
        {
            try {
                var line = CommandJsonSerializer.SerializeToLine(data);
                Output(this, line);
                await transport.SendLine(line);
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
        public string ServerSpringVersion {get { return ServerWelcome != null ? ServerWelcome.Engine : null; }  }

        public string UserName { get; private set; }
        public string UserPassword { get; private set; }

        public event EventHandler<string> Input = delegate {};
        public event EventHandler<string> Output = delegate {};
        public event EventHandler<User> UserAdded = delegate { };
        public event EventHandler<UserDisconnected> UserRemoved = delegate { };
        public event EventHandler<OldNewPair<User>> UserStatusChanged = delegate { };
        public event EventHandler<OldNewPair<User>> MyUserStatusChanged = delegate { };

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
        public event EventHandler<UserBattleStatus> BattleUserStatusChanged = delegate { };
        public event EventHandler<UserBattleStatus> BattleMyUserStatusChanged = delegate { };
        public event EventHandler<Channel> ChannelJoined = delegate { };
        public event EventHandler<Channel> ChannelLeft = delegate { };
        public event EventHandler<BotBattleStatus> BattleBotAdded = delegate { };
        public event EventHandler<BotBattleStatus> BattleBotUpdated = delegate { };
        public event EventHandler<BotBattleStatus> BattleBotRemoved = delegate { };
        public event EventHandler<TasEventArgs> ConnectionLost = delegate { };
        public event EventHandler<Say> Rang = delegate { };
        public event EventHandler<CancelEventArgs<TasSayEventArgs>> PreviewSaid = delegate { };
        public event EventHandler<Battle> MyBattleHostExited = delegate { };
        public event EventHandler<Battle> MyBattleStarted = delegate { };
        public event EventHandler<SetRectangle> StartRectAdded = delegate { };
        public event EventHandler<SetRectangle> StartRectRemoved = delegate { };
        public event EventHandler<OldNewPair<Battle>> BattleInfoChanged = delegate { };
        public event EventHandler<OldNewPair<Battle>> BattleMapChanged = delegate { };
        public event EventHandler<OldNewPair<Battle>> MyBattleMapChanged = delegate { };
        public event EventHandler<Battle> ModOptionsChanged = delegate { };

        public event EventHandler<SiteToLobbyCommand> SiteToLobbyCommandReceived = delegate { };

        
        public event EventHandler<ChangeTopic> ChannelTopicChanged = delegate { };
        
        
        
 

        public TasClient(string appName, Login.ClientTypes? clientTypes = null, string ipOverride = null)
        {
            if (clientTypes.HasValue) this.clientType = clientTypes.Value;
            this.appName = appName;
            

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
        }


        public async Task AddBattleRectangle(int allyno, BattleRect rect)
        {
            {
                if (allyno < Spring.MaxAllies && allyno >= 0) {
                    await SendCommand(new SetRectangle() { Number = allyno, Rectangle = rect });
                }
            }
        }

        private async Task Process(SetRectangle rect)
        {
            var bat = MyBattle;
            if (bat != null) {
                if (rect.Rectangle == null) {
                    BattleRect org;
                    bat.Rectangles.TryRemove(rect.Number, out org);
                    StartRectAdded(this, rect);
                } else {
                    bat.Rectangles[rect.Number] = rect.Rectangle;
                    StartRectRemoved(this, rect);
                }
            }
        }

        private async Task Process(SetModOptions options)
        {
            var bat = MyBattle;
            if (bat != null) {
                bat.ModOptions = options.Options;
                ModOptionsChanged(this, bat);
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


        public Task ChangeMap(string name)
        {
            return SendCommand(new BattleUpdate() { Header = new BattleHeader() { BattleID = MyBattleID, Map = name } });
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


        public Task ChangeMyUserStatus(bool? isAway = null, bool? isInGame = null)
        {
            return SendCommand(new ChangeUserStatus() { IsAfk = isAway, IsInGame = isInGame });
        }

        SynchronizationContext context;

        public void Connect(string host, int port)
        {
            context = SynchronizationContext.Current;
            serverHost = host;
            serverPort = port;
            WasDisconnectRequested = false;
            pingTimer.Start();

            var con = new TcpTransport(host,port, forcedLocalIP ? localIp:null);
            transport = con;
            con.ConnectAndRun(OnCommandReceived, OnConnected, OnConnectionClosed);
        }

        public bool WasDisconnectRequested { get; private set; }
        public void RequestDisconnect()
        {
            WasDisconnectRequested = true;
            transport.RequestClose();
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
            return SendCommand(new JoinChannel() { ChannelName = channelName, Password = key });
        }

        public Task Kick(string username, int? battleID=null, string reason = null)
        {
            return SendCommand(new KickFromBattle() { Name = username, BattleID = battleID, Reason = reason });
        }

        public Task AdminKickFromLobby(string username,string reason)
        {
            return SendCommand(new KickFromServer() { Name = username, Reason = reason });
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
                await SendCommand(new LeaveChannel() { ChannelName = channelName });
            }
        }


        public static long GetMyUserID() {
            var nics = NetworkInterface.GetAllNetworkInterfaces().Where(x=> !String.IsNullOrWhiteSpace(x.GetPhysicalAddress().ToString())
                && x.NetworkInterfaceType != NetworkInterfaceType.Loopback && x.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

            var wantedNic = nics.FirstOrDefault();

            if (wantedNic != null)
            {
                return Crc.Crc32(wantedNic.GetPhysicalAddress().GetAddressBytes());
            }
            return 0;
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
            if (MyBattle != null) LeaveBattle();

            return SendCommand(new OpenBattle() {
                Header =
                    new BattleHeader() {
                        Engine = nbattle.EngineVersion,
                        Game = nbattle.ModName,
                        Ip = nbattle.Ip ?? localIp,
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

        public async Task RemoveBattleRectangle(int allyno)
        {
            if (MyBattle.Rectangles.ContainsKey(allyno)) {
                await SendCommand(new SetRectangle() { Number = allyno, Rectangle = null });
            }
        }

        public async Task RemoveBot(string name)
        {
            var bat = MyBattle;
            if (bat != null && bat.Bots.ContainsKey(name)) await SendCommand(new RemoveBot{Name = name});
        }



        public Task Ring(SayPlace place, string channel, string text = null)
        {
            return Say(place, channel, text ?? ("Ringing " + channel), false, isRing: true);
        }


        public async Task ForceJoinBattle(string name, string battleHostName) {
            var battle = ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == battleHostName);
            if (battle != null) await ForceJoinBattle(name, battle.BattleID);
        }

        public Task ForceJoinBattle(string name, int battleID)
        {
            return SendCommand(new ForceJoinBattle() { Name = name, BattleID = battleID });
        }

        public Task ForceJoinChannel(string user, string channel)
        {
            return SendCommand(new ForceJoinChannel() { UserName = user, ChannelName = channel });
        }

        public Task ForceLeaveChannel(string user, string channel, string reason = null)
        {
            return SendCommand(new KickFromChannel() { ChannelName = channel, UserName = user, Reason = reason });
        }


        /// <summary>
        /// Say something through chat system
        /// </summary>
        /// <param Name="place">Pick user (private message) channel or battle</param>
        /// <param Name="channel">Channel or User Name</param>
        /// <param Name="inputtext">chat text</param>
        /// <param Name="isEmote">is message emote? (channel or battle only)</param>
        /// <param Name="linePrefix">text to be inserted in front of each line (example: "!pm xyz")</param>
        public async Task Say(SayPlace place, string channel, string inputtext, bool isEmote, bool isRing = false)
        {
            if (String.IsNullOrEmpty(inputtext)) return;
            var lines = inputtext.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var text in lines)
            {
                if (String.IsNullOrEmpty(text)) continue;

                var args = new SayingEventArgs(place, channel, text, isEmote);
                Saying(this, args);
                if (args.Cancel) continue;

                if (args.SayPlace == SayPlace.Channel && !JoinedChannels.ContainsKey(args.Channel)) {
                    await JoinChannel(args.Channel);
                }

                var say = new Say() { Target = args.Channel, Place = args.SayPlace, Text = args.Text, IsEmote = args.IsEmote, Ring = isRing};

                await SendCommand(say);
            }
        }



        public Task SendRaw(string text)
        {
            if (!text.EndsWith("\n")) text += "\n";
            Output(this, text);
            return transport.SendLine(text);
        }

        public Task SetModOptions(Dictionary<string,string> data)
        {
            return SendCommand(new SetModOptions() { Options = data });
        }

        public Task UpdateModOptions(Dictionary<string, string> data)
        {
            var cur = new Dictionary<string, string>(MyBattle.ModOptions);
            foreach (var d in data) {
                cur[d.Key] = d.Value;
            }
            return SetModOptions(cur);
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

        User UserGetter(string n)
        {
            User us;
            if (existingUsers.TryGetValue(n, out us)) return us;
            else return new User() { Name = n };
        }


        async Task Process(BattleAdded bat)
        {
            var newBattle = new Battle();
            newBattle.UpdateWith(bat.Header, UserGetter);
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
            User user;
            Battle bat;
            
            existingUsers.TryGetValue(left.User, out user);
            existingBattles.TryGetValue(left.BattleID, out bat);

            if (bat != null && user != null) {
                user.IsInBattleRoom = false;
                UserBattleStatus removed;
                bat.Users.TryRemove(left.User, out removed);

                if (MyBattle != null && left.BattleID == MyBattleID) {
                    if (UserName == left.User) {
                        bat.Rectangles.Clear();
                        bat.Bots.Clear();
                        bat.ModOptions.Clear();
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
            
            existingBattles.Remove(br.BattleID);
            if (battle == MyBattle)
            {
                BattleClosed(this, battle);
                MyBattleRemoved(this, battle);
            }
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

        async Task Process(Ping ping)
        {
            lastPing = DateTime.UtcNow;
        }

        async Task Process(User userUpdate)
        {
            User user;
            User old = null;
            existingUsers.TryGetValue(userUpdate.Name, out user);
            if (user != null) {
                old = user.Clone();
                user.UpdateWith(userUpdate);
            } else user = userUpdate;
            existingUsers[user.Name] = user;

            if (old == null) UserAdded(this, user);
            if (old != null) {
                var bat = MyBattle;
                if (bat != null && bat.Founder.Name == user.Name)
                {
                    if (user.IsInGame && !old.IsInGame) MyBattleStarted(this,bat );
                    if (!user.IsInGame && old.IsInGame) MyBattleHostExited(this, bat);
                }
            }
            if (user.Name == UserName) MyUserStatusChanged(this, new OldNewPair<User>(old,user));
            UserStatusChanged(this, new OldNewPair<User>(old, user));
        }


        async Task Process(SiteToLobbyCommand command)
        {
            SiteToLobbyCommandReceived(this, command);
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
            InvokeSaid(new TasSayEventArgs(say.Place, say.Target,say.User, say.Text, say.IsEmote) {Time = say.Time});
            if (say.Ring) Rang(this, say);
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
                    Name = response.Channel.ChannelName,
                    Topic = response.Channel.Topic,
                };
                
                JoinedChannels[response.ChannelName] = chan;

                foreach (var u in response.Channel.Users) {
                    User user;
                    if (existingUsers.TryGetValue(u, out user)) chan.Users[u] = user;
                }
                
                var cancelEvent = new CancelEventArgs<Channel>(chan);
                PreviewChannelJoined(this, cancelEvent);
                if (!cancelEvent.Cancel) {
                    ChannelJoined(this, chan);
                    ChannelUserAdded(this, new ChannelUserInfo() {Channel = chan, Users = chan.Users.Values.ToList()});
                    if (!string.IsNullOrEmpty(chan.Topic.Text)) {
                        ChannelTopicChanged(this, new ChangeTopic() {
                            ChannelName = chan.Name,
                            Topic = chan.Topic
                        });
                    }
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
            existingUsers.Remove(arg.Name);
            UserRemoved(this, arg);
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


        async Task Process(BattleUpdate batUp)
        {
            var h = batUp.Header;
            Battle bat;
            if (existingBattles.TryGetValue(h.BattleID.Value, out bat)) {
                var org = bat.Clone();
                bat.UpdateWith(h, UserGetter);
                var pair = new OldNewPair<Battle>(org, bat);
                if (org.MapName != bat.MapName) {
                    if (bat == MyBattle) MyBattleMapChanged(this, pair);
                    BattleMapChanged(this, pair);
                }
                BattleInfoChanged(this, pair);
            }
        }


        async Task Process(ChangeTopic changeTopic)
        {
            Channel chan;
            if (joinedChannels.TryGetValue(changeTopic.ChannelName, out chan)) {
                chan.Topic = changeTopic.Topic;
            }
            ChannelTopicChanged(this, changeTopic);
        }

        
        void InvokeSaid(TasSayEventArgs sayArgs)
        {
            var previewSaidEventArgs = new CancelEventArgs<TasSayEventArgs>(sayArgs);
            PreviewSaid(this, previewSaidEventArgs);
            if (!previewSaidEventArgs.Cancel) Said(this, sayArgs);
        }


        DateTime lastPing;

        void OnPingTimer(object sender, EventArgs args)
        {
            if (context != null) context.Post(x=>PingTimerInternal(),null);
            else PingTimerInternal();
        }

        private void PingTimerInternal()
        {
            if (IsConnected) SendCommand(new Ping());
            else if(!WasDisconnectRequested) Connect(serverHost, serverPort);
        }

        public Task LinkSteam(string token)
        {
            return SendCommand(new LinkSteam() { Token = token });
        }
    }
}