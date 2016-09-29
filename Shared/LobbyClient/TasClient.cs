#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PlasmaShared;
using ZkData;
using Timer = System.Timers.Timer;

#endregion

namespace LobbyClient
{
    public class TasClient
    {
        public delegate void Invoker();

        public delegate void Invoker<TArg>(TArg arg);

        public const int MaxAlliances = 16;
        public const int MaxTeams = 16;

        private static CommandJsonSerializer CommandJsonSerializer = new CommandJsonSerializer(Utils.GetAllTypesWithAttribute<MessageAttribute>());


        private readonly string appName = "UnknownClient";
        private readonly bool forcedLocalIP = false;
        private readonly string localIp;
        private readonly Timer pingTimer;


        private Login.ClientTypes clientType = LobbyClient.Login.ClientTypes.ZeroKLobby |
                                               (Environment.OSVersion.Platform == PlatformID.Unix ? LobbyClient.Login.ClientTypes.Linux : 0);

        private SynchronizationContext context;
        private List<string> friends = new List<string>();


        private List<string> ignores = new List<string>();


        private DateTime lastPing;

        private int lastUdpSourcePort;
        private int lastUserStatus;

        private int pingInterval = 30; // how often to ping server (in seconds)

        private int serverPort;


        public Welcome ServerWelcome = new Welcome();
        private ITransport transport;
        public Dictionary<int, Battle> ExistingBattles { get; set; } = new Dictionary<int, Battle>();

        public Dictionary<string, User> ExistingUsers { get; set; } = new Dictionary<string, User>();
        public IReadOnlyCollection<string> Friends => friends.AsReadOnly();
        public IReadOnlyCollection<string> Ignores => ignores.AsReadOnly();

        public bool IsConnected { get { return (transport != null) && transport.IsConnected; } }

        public bool IsLoggedIn { get; private set; }

        public Dictionary<string, Channel> JoinedChannels { get; private set; } = new Dictionary<string, Channel>();

        public List<string> MatchMakerJoinedQueues { get; private set; } = new List<string>();

        public Battle MyBattle { get; protected set; }

        public int MyBattleID
        {
            get
            {
                var bat = MyBattle;
                if (bat != null) return bat.BattleID;
                else return 0;
            }
        }


        public UserBattleStatus MyBattleStatus
        {
            get
            {
                if (MyBattle != null)
                {
                    UserBattleStatus ubs;
                    MyBattle.Users.TryGetValue(UserName, out ubs);
                    return ubs;
                }
                return null;
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
                pingTimer.Interval = pingInterval * 1000;
            }
        }


        public List<MatchMakerSetup.Queue> PossibleQueues { get; private set; } = new List<MatchMakerSetup.Queue>();
        public string serverHost { get; private set; }
        public string ServerSpringVersion { get { return ServerWelcome != null ? ServerWelcome.Engine : null; } }

        public string UserName { get; private set; }
        public string UserPassword { get; private set; }

        public bool WasDisconnectRequested { get; private set; }

        public TasClient(string appName, Login.ClientTypes? clientTypes = null, string ipOverride = null)
        {
            if (clientTypes.HasValue) clientType = clientTypes.Value;
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
                    if (adr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIp = adr.ToString();
                        break;
                    }
            }

            pingTimer = new Timer(pingInterval * 1000) { AutoReset = true };
            pingTimer.Elapsed += OnPingTimer;
        }


        public Task AddBot(string name, string aiDll, int? allyNumber = null)
        {
            var u = new UpdateBotStatus();
            if (aiDll != null) u.AiLib = aiDll;
            if (name != null) u.Name = name;
            if (allyNumber != null) u.AllyNumber = allyNumber;
            return SendCommand(u);
        }

        public Task AdminKickFromLobby(string username, string reason)
        {
            return SendCommand(new KickFromServer() { Name = username, Reason = reason });
        }

        public void AdminSetChannelPassword(string channel, string password)
        {
            if (string.IsNullOrEmpty(password)) Say(SayPlace.User, "ChanServ", string.Format("!lock #{0} {1}", channel, password), false);
            else Say(SayPlace.User, "ChanServ", string.Format("!unlock #{0}", channel), false);
        }

        public void AdminSetTopic(string channel, string topic)
        {
            Say(SayPlace.User, "ChanServ", string.Format("!topic #{0} {1}", channel, topic.Replace("\n", "\\n")), false);
        }

        public event EventHandler<AreYouReady> AreYouReadyStarted = delegate { };
        public event EventHandler<AreYouReadyUpdate> AreYouReadyUpdated = delegate { };
        public event EventHandler<AreYouReadyResult> AreYouReadyClosed = delegate { };



        public Task AreYouReadyResponse(bool ready)
        {
            return SendCommand(new AreYouReadyResponse() { Ready = ready });
        }

        public event EventHandler<BotBattleStatus> BattleBotAdded = delegate { };
        public event EventHandler<BotBattleStatus> BattleBotRemoved = delegate { };
        public event EventHandler<BotBattleStatus> BattleBotUpdated = delegate { };
        public event EventHandler<Battle> BattleClosed = delegate { };

        public event EventHandler<Battle> BattleFound = delegate { };
        public event EventHandler<OldNewPair<Battle>> BattleInfoChanged = delegate { };
        public event EventHandler<Battle> BattleJoined = delegate { };
        public event EventHandler<OldNewPair<Battle>> BattleMapChanged = delegate { };
        public event EventHandler<UserBattleStatus> BattleMyUserStatusChanged = delegate { };
        public event EventHandler<Battle> BattleOpened = delegate { };
        public event EventHandler<Battle> BattleRemoved = delegate { }; // raised just after the battle is removed from the battle list
        public event EventHandler<BattleUserEventArgs> BattleUserJoined = delegate { };
        public event EventHandler<BattleUserEventArgs> BattleUserLeft = delegate { };
        public event EventHandler<UserBattleStatus> BattleUserStatusChanged = delegate { };


        public Task ChangeMap(string name)
        {
            return SendCommand(new BattleUpdate() { Header = new BattleHeader() { BattleID = MyBattleID, Map = name } });
        }

        public async Task ChangeMyBattleStatus(bool? spectate = null, SyncStatuses? syncStatus = null, int? ally = null)
        {
            var ubs = MyBattleStatus;
            if (ubs != null)
            {
                var status = new UpdateUserBattleStatus() { IsSpectator = spectate, Sync = syncStatus, AllyNumber = ally, Name = UserName };
                await SendCommand(status);
            }
        }


        public Task ChangeMyUserStatus(bool? isAway = null, bool? isInGame = null)
        {
            return SendCommand(new ChangeUserStatus() { IsAfk = isAway, IsInGame = isInGame });
        }

        public event EventHandler<Channel> ChannelJoined = delegate { };
        public event EventHandler<JoinChannelResponse> ChannelJoinFailed = delegate { };
        public event EventHandler<Channel> ChannelLeft = delegate { };


        public event EventHandler<ChangeTopic> ChannelTopicChanged = delegate { };
        public event EventHandler<ChannelUserInfo> ChannelUserAdded = delegate { };
        public event EventHandler<ChannelUserRemovedInfo> ChannelUserRemoved = delegate { };

        public void Connect(string host, int port)
        {
            context = SynchronizationContext.Current;
            serverHost = host;
            serverPort = port;
            WasDisconnectRequested = false;
            pingTimer.Start();

            var con = new TcpTransport(host, port, forcedLocalIP ? localIp : null);
            transport = con;
            con.ConnectAndRun(OnCommandReceived, OnConnected, OnConnectionClosed);
        }

        public event EventHandler Connected = delegate { };
        public event EventHandler<TasEventArgs> ConnectionLost = delegate { };
        public event EventHandler<ConnectSpring> ConnectSpringReceived = delegate { };
        public event EventHandler<Welcome> WelcomeReceived = delegate { };


        public async Task ForceAlly(string username, int ally)
        {
            if ((MyBattle != null) && MyBattle.Users.ContainsKey(username))
            {
                var ubs = new UpdateUserBattleStatus() { Name = username, AllyNumber = ally };
                await SendCommand(ubs);
            }
        }


        public async Task ForceJoinBattle(string name, string battleHostName)
        {
            var battle = ExistingBattles.Values.FirstOrDefault(x => x.FounderName == battleHostName);
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

        public async Task ForceSpectator(string username, bool spectatorState = true)
        {
            if ((MyBattle != null) && MyBattle.Users.ContainsKey(username))
            {
                var ubs = new UpdateUserBattleStatus() { Name = username, IsSpectator = spectatorState };
                await SendCommand(ubs);
            }
        }

        public event EventHandler<IReadOnlyCollection<string>> FriendListUpdated = delegate { };


        public bool GetExistingUser(string name, out User u)
        {
            return ExistingUsers.TryGetValue(name, out u);
        }


        public static long GetMyUserID()
        {
            var nics =
                NetworkInterface.GetAllNetworkInterfaces()
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x.GetPhysicalAddress().ToString()) &&
                            (x.NetworkInterfaceType != NetworkInterfaceType.Loopback) && (x.NetworkInterfaceType != NetworkInterfaceType.Tunnel));

            var wantedNic = nics.FirstOrDefault();

            if (wantedNic != null) return Crc.Crc32(wantedNic.GetPhysicalAddress().GetAddressBytes());
            return 0;
        }

        public User GetUserCaseInsensitive(string userName)
        {
            return ExistingUsers.Values.FirstOrDefault(u => string.Equals(u.Name, userName, StringComparison.InvariantCultureIgnoreCase));
        }

        public event EventHandler<IReadOnlyCollection<string>> IgnoreListUpdated = delegate { };

        public event EventHandler<string> Input = delegate { };


        public Task JoinBattle(int battleID, string password = null)
        {
            return SendCommand(new JoinBattle() { BattleID = battleID, Password = password });
        }


        public Task JoinChannel(string channelName, string key = null)
        {
            return SendCommand(new JoinChannel() { ChannelName = channelName, Password = key });
        }

        public Task Kick(string username, int? battleID = null, string reason = null)
        {
            return SendCommand(new KickFromBattle() { Name = username, BattleID = battleID, Reason = reason });
        }


        public async Task LeaveBattle()
        {
            if (MyBattle != null) await SendCommand(new LeaveBattle() { BattleID = MyBattle.BattleID });
        }


        public async Task LeaveChannel(string channelName)
        {
            if (JoinedChannels.ContainsKey(channelName)) await SendCommand(new LeaveChannel() { ChannelName = channelName });
        }

        public Task LinkSteam(string token)
        {
            return SendCommand(new LinkSteam() { Token = token });
        }


        public Task Login(string userName, string password)
        {
            UserName = userName;
            UserPassword = password;
            return
                SendCommand(new Login()
                {
                    Name = userName,
                    PasswordHash = Utils.HashLobbyPassword(password),
                    ClientType = clientType,
                    UserID = GetMyUserID(),
                    LobbyVersion = appName
                });
        }

        public event EventHandler<TasEventArgs> LoginAccepted = delegate { };
        public event EventHandler<LoginResponse> LoginDenied = delegate { };


        public Task MatchMakerQueueRequest(IEnumerable<string> names)
        {
            return SendCommand(new MatchMakerQueueRequest() { Queues = names?.ToList() });
        }

        public event EventHandler<MatchMakerSetup> MatchMakerSetupReceived = delegate { };
        public event EventHandler<MatchMakerStatus> MatchMakerStatusUpdated = delegate { };
        public event EventHandler<Battle> ModOptionsChanged = delegate { };
        public event EventHandler<Battle> MyBattleHostExited = delegate { };
        public event EventHandler<OldNewPair<Battle>> MyBattleMapChanged = delegate { };
        public event EventHandler<Battle> MyBattleRemoved = delegate { }; // raised just after the battle is removed from the battle list
        public event EventHandler<Battle> MyBattleStarted = delegate { };
        public event EventHandler<OldNewPair<User>> MyUserStatusChanged = delegate { };

        public async Task OnCommandReceived(string line)
        {
            try
            {
                Input(this, line);
                dynamic obj = CommandJsonSerializer.DeserializeLine(line);
                await Process(obj);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing line {0} : {1}", line, ex);
            }
        }


        public async Task OnConnected()
        {
            MyBattle = null;
            ExistingUsers = new Dictionary<string, User>();
            JoinedChannels = new Dictionary<string, Channel>();
            ExistingBattles = new Dictionary<int, Battle>();
            IsLoggedIn = false;
            Connected(this, EventArgs.Empty);
        }

        public async Task OnConnectionClosed(bool wasRequested)
        {
            ExistingUsers = new Dictionary<string, User>();
            JoinedChannels = new Dictionary<string, Channel>();
            ExistingBattles = new Dictionary<int, Battle>();

            MyBattle = null;
            IsLoggedIn = false;
            ConnectionLost(this, new TasEventArgs(string.Format("Connection {0}", wasRequested ? "closed on user request" : "disconnected")));
        }

        public Task OpenBattle(BattleHeader header)
        {
            if (MyBattle != null) LeaveBattle();
            return SendCommand(new OpenBattle() { Header = header });
        }

        public event EventHandler<string> Output = delegate { };
        public event EventHandler<CancelEventArgs<Channel>> PreviewChannelJoined = delegate { };
        public event EventHandler<CancelEventArgs<TasSayEventArgs>> PreviewSaid = delegate { };


        public async Task Process(UpdateBotStatus status)
        {
            var bat = MyBattle;
            if (bat != null)
            {
                BotBattleStatus ubs;
                bat.Bots.TryGetValue(status.Name, out ubs);
                if (ubs != null)
                {
                    ubs.UpdateWith(status);
                    BattleBotUpdated(this, ubs);
                }
                else
                {
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
                if (bat.Bots.TryRemove(status.Name, out ubs)) BattleBotRemoved(this, ubs);
            }
        }

        public event EventHandler<Say> Rang = delegate { };


        public Task Register(string username, string password)
        {
            return SendCommand(new Register() { Name = username, PasswordHash = Utils.HashLobbyPassword(password) });
        }

        public event EventHandler<TasEventArgs> RegistrationAccepted = delegate { };
        public event EventHandler<RegisterResponse> RegistrationDenied = delegate { };

        public async Task RemoveBot(string name)
        {
            var bat = MyBattle;
            if ((bat != null) && bat.Bots.ContainsKey(name)) await SendCommand(new RemoveBot { Name = name });
        }

        public Task RequestConnectSpring(int battleID)
        {
            return SendCommand(new RequestConnectSpring() { BattleID = battleID });
        }

        public void RequestDisconnect()
        {
            WasDisconnectRequested = true;
            transport?.RequestClose();
        }


        public Task Ring(SayPlace place, string channel, string text = null)
        {
            return Say(place, channel, text ?? "Ringing " + channel, false, true);
        }

        public event EventHandler<TasSayEventArgs> Said = delegate { }; // this is fired when any kind of say message is recieved


        /// <summary>
        ///     Say something through chat system
        /// </summary>
        /// <param Name="place">Pick user (private message) channel or battle</param>
        /// <param Name="channel">Channel or User Name</param>
        /// <param Name="inputtext">chat text</param>
        /// <param Name="isEmote">is message emote? (channel or battle only)</param>
        /// <param Name="linePrefix">text to be inserted in front of each line (example: "!pm xyz")</param>
        public async Task Say(SayPlace place, string channel, string inputtext, bool isEmote, bool isRing = false)
        {
            if (string.IsNullOrEmpty(inputtext)) return;
            var lines = inputtext.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var text in lines)
            {
                if (string.IsNullOrEmpty(text)) continue;

                var args = new SayingEventArgs(place, channel, text, isEmote);
                Saying(this, args);
                if (args.Cancel) continue;

                if ((args.SayPlace == SayPlace.Channel) && !JoinedChannels.ContainsKey(args.Channel)) await JoinChannel(args.Channel);

                var say = new Say() { Target = args.Channel, Place = args.SayPlace, Text = args.Text, IsEmote = args.IsEmote, Ring = isRing };

                await SendCommand(say);
            }
        }

        public event EventHandler<SayingEventArgs> Saying = delegate { }; // this client is trying to say somethign

        public async Task SendCommand<T>(T data)
        {
            try
            {
                var line = CommandJsonSerializer.SerializeToLine(data);
                Output(this, line);
                await transport.SendLine(line);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error sending {0} : {1}", data, ex);
            }
        }


        public Task SendRaw(string text)
        {
            if (!text.EndsWith("\n")) text += "\n";
            Output(this, text);
            return transport.SendLine(text);
        }

        public Task SetModOptions(Dictionary<string, string> data)
        {
            return SendCommand(new SetModOptions() { Options = data });
        }

        public Task SetRelation(string target, Relation rel)
        {
            if (rel == Relation.Friend) if (!friends.Contains(target)) return SendCommand(new SetAccountRelation() { Relation = rel, TargetName = target });
            if (rel == Relation.Ignore) if (!ignores.Contains(target)) return SendCommand(new SetAccountRelation() { Relation = rel, TargetName = target });
            return SendCommand(new SetAccountRelation() { Relation = rel, TargetName = target });
        }

        public event EventHandler<SiteToLobbyCommand> SiteToLobbyCommandReceived = delegate { };


        /// <summary>
        ///     Starts game and automatically does hole punching if necessary
        /// </summary>
        public void StartGame()
        {
            ChangeMyUserStatus(false, true);
        }

        public async Task UpdateBot(string name, string aiDll, int? allyNumber = null)
        {
            var bat = MyBattle;
            if ((bat != null) && bat.Bots.ContainsKey(name)) await AddBot(name, aiDll, allyNumber);
        }

        public Task UpdateModOptions(Dictionary<string, string> data)
        {
            var cur = new Dictionary<string, string>(MyBattle.ModOptions);
            foreach (var d in data) cur[d.Key] = d.Value;
            return SetModOptions(cur);
        }

        public event EventHandler<User> UserAdded = delegate { };
        public event EventHandler<UserDisconnected> UserRemoved = delegate { };
        public event EventHandler<OldNewPair<User>> UserStatusChanged = delegate { };


        private void InvokeSaid(TasSayEventArgs sayArgs)
        {
            var previewSaidEventArgs = new CancelEventArgs<TasSayEventArgs>(sayArgs);
            PreviewSaid(this, previewSaidEventArgs);
            if (!previewSaidEventArgs.Cancel) Said(this, sayArgs);
        }

        private void OnPingTimer(object sender, EventArgs args)
        {
            if (context != null) context.Post(x => PingTimerInternal(), null);
            else PingTimerInternal();
        }

        private void PingTimerInternal()
        {
            if (IsConnected) SendCommand(new Ping());
            else if (!WasDisconnectRequested) Connect(serverHost, serverPort);
        }


        private async Task Process(SetModOptions options)
        {
            var bat = MyBattle;
            if (bat != null)
            {
                bat.ModOptions = options.Options;
                ModOptionsChanged(this, bat);
            }
        }

        private async Task Process(BattleAdded bat)
        {
            var newBattle = new Battle();
            newBattle.UpdateWith(bat.Header);
            ExistingBattles[newBattle.BattleID] = newBattle;
            //newBattle.Founder.IsInBattleRoom = true;

            BattleFound(this, newBattle);
        }

        private async Task Process(MatchMakerSetup setup)
        {
            PossibleQueues = setup.PossibleQueues;
            MatchMakerSetupReceived(this, setup);
        }

        private async Task Process(AreYouReady areYou)
        {
            AreYouReadyStarted(this, areYou);
        }

        private async Task Process(AreYouReadyUpdate areYou)
        {
            AreYouReadyUpdated(this, areYou);
        }

        private async Task Process(AreYouReadyResult areYou)
        {
            AreYouReadyClosed(this, areYou);
        }


        private async Task Process(MatchMakerStatus status)
        {
            MatchMakerJoinedQueues = status.JoinedQueues;
            MatchMakerStatusUpdated(this, status);
        }

        private async Task Process(JoinedBattle bat)
        {
            User user;
            ExistingUsers.TryGetValue(bat.User, out user);
            Battle battle;
            ExistingBattles.TryGetValue(bat.BattleID, out battle);
            if ((user != null) && (battle != null))
            {
                battle.Users[user.Name] = new UserBattleStatus(user.Name, user);
                user.IsInBattleRoom = true;
                BattleUserJoined(this, new BattleUserEventArgs(user.Name, bat.BattleID));
                if (user.Name == UserName)
                {
                    MyBattle = battle;
                    if (battle.FounderName == UserName) BattleOpened(this, battle);
                    BattleJoined(this, MyBattle);
                }
            }
        }


        private async Task Process(LeftBattle left)
        {
            User user;
            Battle bat;

            ExistingUsers.TryGetValue(left.User, out user);
            ExistingBattles.TryGetValue(left.BattleID, out bat);

            if ((bat != null) && (user != null))
            {
                user.IsInBattleRoom = false;
                UserBattleStatus removed;
                bat.Users.TryRemove(left.User, out removed);

                if ((MyBattle != null) && (left.BattleID == MyBattleID))
                    if (UserName == left.User)
                    {
                        bat.Bots.Clear();
                        bat.ModOptions.Clear();
                        MyBattle = null;
                        BattleClosed(this, bat);
                    }
                BattleUserLeft(this, new BattleUserEventArgs(user.Name, left.BattleID));
            }
        }

        private async Task Process(BattleRemoved br)
        {
            Battle battle;
            if (!ExistingBattles.TryGetValue(br.BattleID, out battle)) return;
            foreach (var u in battle.Users.Keys)
            {
                User user;
                if (ExistingUsers.TryGetValue(u, out user)) user.IsInBattleRoom = false;
            }

            ExistingBattles.Remove(br.BattleID);
            if (battle == MyBattle)
            {
                BattleClosed(this, battle);
                MyBattleRemoved(this, battle);
            }
            BattleRemoved(this, battle);
        }


        private async Task Process(LoginResponse loginResponse)
        {
            if (loginResponse.ResultCode == LoginResponse.Code.Ok)
            {
                IsLoggedIn = true;
                LoginAccepted(this, new TasEventArgs());
            }
            else
            {
                IsLoggedIn = false;
                LoginDenied(this, loginResponse);
            }
        }

        private async Task Process(Ping ping)
        {
            lastPing = DateTime.UtcNow;
        }

        private async Task Process(User userUpdate)
        {
            User user;
            User old = null;
            ExistingUsers.TryGetValue(userUpdate.Name, out user);
            if (user != null)
            {
                old = user.Clone();
                user.UpdateWith(userUpdate);
            }
            else user = userUpdate;
            ExistingUsers[user.Name] = user;

            if (old == null) UserAdded(this, user);
            if (old != null)
            {
                var bat = MyBattle;
                if ((bat != null) && (bat.FounderName == user.Name))
                {
                    if (user.IsInGame && !old.IsInGame) MyBattleStarted(this, bat);
                    if (!user.IsInGame && old.IsInGame) MyBattleHostExited(this, bat);
                }
            }
            if (user.Name == UserName) MyUserStatusChanged(this, new OldNewPair<User>(old, user));
            UserStatusChanged(this, new OldNewPair<User>(old, user));
        }


        private async Task Process(SiteToLobbyCommand command)
        {
            SiteToLobbyCommandReceived(this, command);
        }


        private async Task Process(RegisterResponse registerResponse)
        {
            if (registerResponse.ResultCode == RegisterResponse.Code.Ok) RegistrationAccepted(this, new TasEventArgs());
            else RegistrationDenied(this, registerResponse);
        }

        private async Task Process(Say say)
        {
            InvokeSaid(new TasSayEventArgs(say.Place, say.Target, say.User, say.Text, say.IsEmote) { Time = say.Time });
            if (say.Ring) Rang(this, say);
        }


        private async Task Process(Welcome welcome)
        {
            ServerWelcome = welcome;
            WelcomeReceived(this, welcome);
        }


        private async Task Process(FriendList friendList)
        {
            friends = friendList.Friends;
            FriendListUpdated(this, friends);
        }

        private async Task Process(IgnoreList ignoreList)
        {
            ignores = ignoreList.Ignores;
            IgnoreListUpdated(this, Ignores);
        }


        private async Task Process(JoinChannelResponse response)
        {
            if (response.Success)
            {
                var chan = new Channel() { Name = response.Channel.ChannelName, Topic = response.Channel.Topic, };

                JoinedChannels[response.ChannelName] = chan;

                foreach (var u in response.Channel.Users)
                {
                    User user;
                    if (ExistingUsers.TryGetValue(u, out user)) chan.Users[u] = user;
                }

                var cancelEvent = new CancelEventArgs<Channel>(chan);
                PreviewChannelJoined(this, cancelEvent);
                if (!cancelEvent.Cancel)
                {
                    ChannelJoined(this, chan);
                    ChannelUserAdded(this, new ChannelUserInfo() { Channel = chan, Users = chan.Users.Values.ToList() });
                    if (!string.IsNullOrEmpty(chan.Topic.Text)) ChannelTopicChanged(this, new ChangeTopic() { ChannelName = chan.Name, Topic = chan.Topic });
                }
            }
            else
            {
                ChannelJoinFailed(this, response);
            }
        }

        private async Task Process(ChannelUserAdded arg)
        {
            Channel chan;
            if (JoinedChannels.TryGetValue(arg.ChannelName, out chan))
                if (!chan.Users.ContainsKey(arg.UserName))
                {
                    User user;
                    if (ExistingUsers.TryGetValue(arg.UserName, out user))
                    {
                        chan.Users[arg.UserName] = user;
                        ChannelUserAdded(this, new ChannelUserInfo() { Channel = chan, Users = new List<User>() { user } });
                    }
                }
        }

        private async Task Process(ChannelUserRemoved arg)
        {
            Channel chan;
            if (JoinedChannels.TryGetValue(arg.ChannelName, out chan))
            {
                User org;
                if (chan.Users.TryRemove(arg.UserName, out org))
                {
                    if (arg.UserName == UserName) ChannelLeft(this, chan);
                    ChannelUserRemoved(this, new ChannelUserRemovedInfo() { Channel = chan, User = org });
                }
            }
        }

        private async Task Process(UserDisconnected arg)
        {
            ExistingUsers.Remove(arg.Name);
            UserRemoved(this, arg);
        }

        private async Task Process(UpdateUserBattleStatus status)
        {
            var bat = MyBattle;
            if (bat != null)
            {
                UserBattleStatus ubs;
                bat.Users.TryGetValue(status.Name, out ubs);
                if (ubs != null)
                {
                    ubs.UpdateWith(status);
                    if (status.Name == UserName) BattleMyUserStatusChanged(this, ubs);
                    BattleUserStatusChanged(this, ubs);
                }
            }
        }


        private async Task Process(BattleUpdate batUp)
        {
            var h = batUp.Header;
            Battle bat;
            if (ExistingBattles.TryGetValue(h.BattleID.Value, out bat))
            {
                var org = bat.Clone();
                bat.UpdateWith(h);
                var pair = new OldNewPair<Battle>(org, bat);
                if (org.MapName != bat.MapName)
                {
                    if (bat == MyBattle) MyBattleMapChanged(this, pair);
                    BattleMapChanged(this, pair);
                }
                BattleInfoChanged(this, pair);
            }
        }


        private async Task Process(ChangeTopic changeTopic)
        {
            Channel chan;
            if (JoinedChannels.TryGetValue(changeTopic.ChannelName, out chan)) chan.Topic = changeTopic.Topic;
            ChannelTopicChanged(this, changeTopic);
        }


        private async Task Process(ConnectSpring connectSpring)
        {
            ConnectSpringReceived(this, connectSpring);
        }
    }
}