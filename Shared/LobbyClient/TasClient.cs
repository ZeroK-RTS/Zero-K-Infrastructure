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
using System.Timers;
using PlasmaShared;
using ZkData;

#endregion

namespace LobbyClient
{
    public class TasClient
    {
        public const int MaxAlliances = 16;
        public const int MaxTeams = 16;

        public ProtocolExtension Extensions { get; private set; }

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
        Dictionary<int, Battle> existingBattles = new Dictionary<int, Battle>();
        Dictionary<string, ExistingChannel> existingChannels = new Dictionary<string, ExistingChannel>();
        Dictionary<string, User> existingUsers = new Dictionary<string, User>();
        readonly bool forcedLocalIP = false;
        readonly Invoker<Invoker> guiThreadInvoker;
        bool isChanScanning;
        bool isConnected;
        bool isLoggedIn;
        Dictionary<string, Channel> joinedChannels = new Dictionary<string, Channel>();
        int lastSpectatorCount;
        int lastUdpSourcePort;
        int lastUserBattleStatus;
        int lastUserColor;
        int lastUserStatus;
        readonly string localIp;
        bool lockToChangeTo;
        int mapChecksumToChangeTo;
        string mapToChangeTo;
        readonly Timer minuteTimer;

        int pingInterval = 30; // how often to ping server (in seconds)
        readonly Timer pingTimer;
        readonly Random random = new Random();
        string serverHost;

        int serverPort;
        int serverUdpHolePunchingPort;
        string serverVersion;
        bool startingAfterUdpPunch;
        readonly Timer udpPunchingTimer = new Timer(400);

        // user info 
        string username = "";
        public bool ConnectionFailed { get; private set; }

        public Dictionary<int, Battle> ExistingBattles { get { return existingBattles; } set { existingBattles = value; } }

        public Dictionary<string, ExistingChannel> ExistingChannels { get { return existingChannels; } }

        public Dictionary<string, User> ExistingUsers { get { return existingUsers; } set { existingUsers = value; } }

        public bool IsConnected { get { return isConnected; } }

        public bool IsLoggedIn { get { return isLoggedIn; } }

        public Dictionary<string, Channel> JoinedChannels { get { return joinedChannels; } }
        public int MessageID { get; private set; }

        public Battle MyBattle { get; protected set; }
        public int MyBattleID { get; private set; }


        public UserBattleStatus MyBattleStatus
        {
            get
            {
                if (MyBattle != null) return MyBattle.Users.SingleOrDefault(x => x.Name == username);
                else return null;
            }
        }

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
        public event EventHandler<EventArgs<BotBattleStatus>> BattleBotAdded = delegate { };
        public event EventHandler<EventArgs<BotBattleStatus>> BattleBotRemoved = delegate { };
        public event EventHandler<EventArgs<BotBattleStatus>> BattleBotUpdated = delegate { };
        public event EventHandler<EventArgs<Battle>> BattleClosed = delegate { };
        public event EventHandler<TasEventArgs> BattleDetailsChanged = delegate { };
        public event EventHandler<TasEventArgs> BattleDisabledUnitsChanged = delegate { };
        public event EventHandler<EventArgs<Battle>> BattleEnded = delegate { }; // raised just after the battle is removed from the battle list
        public event EventHandler<EventArgs<Battle>> BattleEnding = delegate { }; // raised just before the battle is removed from the battle list
        public event EventHandler BattleForceQuit = delegate { }; // i was kicked from a battle (sent after LEFTBATTLE)
        public event EventHandler<EventArgs<Battle>> BattleFound = delegate { };
        public event EventHandler<BattleInfoEventArgs> BattleInfoChanged = delegate { };
        public event EventHandler<EventArgs<Battle>> BattleJoined = delegate { };
        public event EventHandler<BattleInfoEventArgs> BattleLockChanged = delegate { };
        public event EventHandler<BattleInfoEventArgs> BattleMapChanged = delegate { };
        public event EventHandler<TasEventArgs> BattleMyUserStatusChanged = delegate { };
        public event EventHandler<TasEventArgs> BattleOpenFailed = delegate { };
        public event EventHandler<TasEventArgs> BattleOpened = delegate { };
        public event EventHandler<EventArgs<User>> BattleStarted = delegate { };
        public event EventHandler<TasEventArgs> BattleUserIpRecieved = delegate { };
        public event EventHandler<BattleUserEventArgs> BattleUserJoined = delegate { };
        public event EventHandler<BattleUserEventArgs> BattleUserLeft = delegate { };
        public event EventHandler<TasEventArgs> BattleUserStatusChanged = delegate { };
        public event EventHandler<TasEventArgs> ChannelForceLeave = delegate { }; // i was kicked from a channel
        public event EventHandler<TasEventArgs> ChannelJoinFailed = delegate { };
        public event EventHandler<TasEventArgs> ChannelJoined = delegate { };
        public event EventHandler<CancelEventArgs<string>> ChannelLeaving = delegate { }; // raised before attempting to leave a channel
        public event EventHandler<TasEventArgs> ChannelLeft = delegate { };
        public event EventHandler<TasEventArgs> ChannelListDone = delegate { };
        public event EventHandler<TasEventArgs> ChannelTopicChanged = delegate { };
        public event EventHandler<TasEventArgs> ChannelUserAdded = delegate { };
        public event EventHandler<TasEventArgs> ChannelUserRemoved = delegate { };
        public event EventHandler<TasEventArgs> ChannelUsersAdded = delegate { }; // raised after a group of clients is recieved in a batch
        public event EventHandler<TasEventArgs> Connected = delegate { };
        public event EventHandler<TasEventArgs> ConnectionLost = delegate { };
        public event EventHandler<TasEventArgs> Failure = delegate { }; //this event is fired whenever any failure events fire
        public event EventHandler<CancelEventArgs<string>> FilterBattleByMod;
        public event EventHandler<EventArgs> HourChime = delegate { };
        public event EventHandler<TasInputArgs> Input = delegate { };
        public event EventHandler<TasEventArgs> JoinBattleFailed = delegate { };
        public event EventHandler<TasEventArgs> LoginAccepted = delegate { };
        public event EventHandler<TasEventArgs> LoginDenied = delegate { };
        public event EventHandler<EventArgs<Battle>> MyBattleEnded = delegate { }; // raised just after the battle is removed from the battle list
        public event EventHandler<TasEventArgs> MyBattleHostExited = delegate { };
        public event EventHandler<BattleInfoEventArgs> MyBattleMapChanged = delegate { };
        public event EventHandler<TasEventArgs> MyBattleStarted = delegate { };
        public event EventHandler<EventArgs<KeyValuePair<string, object[]>>> Output = delegate { }; // outgoing command and arguments
        public event EventHandler<CancelEventArgs<TasEventArgs>> PreviewChannelJoined = delegate { };
        public event EventHandler<CancelEventArgs<TasSayEventArgs>> PreviewSaid = delegate { };
        public event EventHandler<EventArgs<string>> Rang = delegate { };
        public event EventHandler<TasEventArgs> Redirect = delegate { };
        public event EventHandler<TasEventArgs> RegistrationAccepted = delegate { };
        public event EventHandler<TasEventArgs> RegistrationDenied = delegate { };
        public event EventHandler RequestBattleStatus = delegate { };
        public event EventHandler<TasSayEventArgs> Said = delegate { }; // this is fired when any kind of say message is recieved
        public event EventHandler<SayingEventArgs> Saying = delegate { }; // this client is trying to say somethign
        public event EventHandler<TasEventArgs> StartRectAdded = delegate { };
        public event EventHandler<TasEventArgs> StartRectRemoved = delegate { };
        public event EventHandler<TasEventArgs> TestLoginAccepted = delegate { };
        public event EventHandler<TasEventArgs> TestLoginDenied = delegate { };
        public event EventHandler<EventArgs<User>> UserAdded = delegate { };
        public event EventHandler<TasEventArgs> UserRemoved = delegate { };
        public event EventHandler<TasEventArgs> UserStatusChanged = delegate { };
        public event EventHandler<EventArgs<User>> UserExtensionsChanged = delegate { };
        public event EventHandler<EventArgs<User>> MyExtensionsChanged = delegate { };
        public event EventHandler<UserLobbyVersionEventArgs> UserLobbyVersionRecieved = delegate { };
        public event EventHandler<UserIPEventArgs> UserIPRecieved = delegate { };
        public event EventHandler<UserIDEventArgs> UserIDRecieved = delegate { }; 

 

        public TasClient(Invoker<Invoker> guiThreadInvoker, string appName, int cpu, bool invokeUserStatusChangedOnExtensions = false, string ipOverride = null)
        {
            this.cpu = cpu;
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
                                                                             u.SetExtension(data);
                                                                             if (invokeUserStatusChangedOnExtensions) UserStatusChanged(this, new TasEventArgs(u.Name, u.ToInt().ToString()));
                                                                             if (user == username) MyExtensionsChanged(this, new EventArgs<User>(u));
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

            udpPunchingTimer.Elapsed += udpPunchingTimer_Elapsed;
            udpPunchingTimer.AutoReset = true;

            ConnectionLost += RaiseFailure;
            LoginDenied += RaiseFailure;
            ChannelJoinFailed += RaiseFailure;
            BattleOpenFailed += RaiseFailure;
        }

        public void AcceptAgreement()
        {
            con.SendCommand("CONFIRMAGREEMENT");
        }

        public void AddBattleRectangle(int allyno, BattleRect rect)
        {
            {
                if (allyno < Spring.MaxAllies && allyno >= 0)
                {
                    RemoveBattleRectangle(allyno);
                    MyBattle.Rectangles.Add(allyno, rect);
                    con.SendCommand("ADDSTARTRECT", allyno, rect.Left, rect.Top, rect.Right, rect.Bottom);
                }
            }
        }

        public void AddBot(string name, UserBattleStatus status, int teamColor, string aiDll)
        {
            if (name.Contains(" ")) throw new TasClientException("Bot name must not contain spaces. " + name);
            con.SendCommand("ADDBOT", name, status.ToInt(), teamColor, aiDll);
        }

        public void ChangeLock(bool lck)
        {
            if (lck != lockToChangeTo) UpdateBattleInfo(lck, mapToChangeTo, mapChecksumToChangeTo);
        }


        public void ChangeMap(string name, int checksum)
        {
            {
                mapToChangeTo = name;
                mapChecksumToChangeTo = checksum;
                UpdateBattleInfo(lockToChangeTo, name, checksum);
            }
        }

        public void ChangeMyBattleStatus(bool? spectate = null,
                                         bool? ready = null,
                                         SyncStatuses? syncStatus = null,
                                         int? side = null,
                                         int? ally = null,
                                         int? team = null)
        {
            var ubs = MyBattleStatus;
            if (ubs != null)
            {
                var clone = (UserBattleStatus)ubs.Clone();
                clone.SetFrom(lastUserBattleStatus, ubs.TeamColor);
                if (spectate.HasValue) clone.IsSpectator = spectate.Value;
                if (ready.HasValue) clone.IsReady = ready.Value;
                if (syncStatus.HasValue) clone.SyncStatus = syncStatus.Value;
                if (side.HasValue) clone.Side = side.Value;
                if (ally.HasValue) clone.AllyNumber = ally.Value;
                if (team.HasValue) clone.TeamNumber = team.Value;
                if (clone.ToInt() != lastUserBattleStatus)
                {
                    SendMyBattleStatus(clone.ToInt(), clone.TeamColor);
                    lastUserBattleStatus = clone.ToInt();
                }
            }
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
            MyBattle = null;
            MyBattleID = 0;
            ExistingUsers = new Dictionary<string, User>();
            existingChannels = new Dictionary<string, ExistingChannel>();
            joinedChannels = new Dictionary<string, Channel>();
            existingBattles = new Dictionary<int, Battle>();
            isChanScanning = false;
            isLoggedIn = false;
            isConnected = false;
            username = "";
            if (con != null) Disconnect();
            try
            {
                con = new ServerConnection();
                con.ConnectionClosed += OnConnectionClosed;
                con.CommandRecieved += OnCommandRecieved;
                con.CommandSent += (s, e) => Output(this, e);
                con.Connect(host, port, forcedLocalIP ? localIp : null);
            }
            catch
            {
                OnConnectionClosed(this, new TasEventArgs("Cannot connect to remote machine"));
            }
        }

        public static DateTime ConvertMilisecondTime(string arg)
        {
            return (new DateTime(1970, 1, 1, 0, 0, 0)).AddMilliseconds(double.Parse(arg));
        }

        public void DisableUnits(params string[] units)
        {
            {
                var temp = new List<string>(units);
                foreach (var s in temp) if (!MyBattle.DisabledUnits.Contains(s)) MyBattle.DisabledUnits.Add(s);
                if (MyBattle.DisabledUnits.Count > 0)
                {
                    con.SendCommand("DISABLEUNITS", MyBattle.DisabledUnits.ToArray());
                    BattleDisabledUnitsChanged(this, new TasEventArgs(MyBattle.DisabledUnits.ToArray()));
                }
            }
        }

        public void Disconnect()
        {
            if (con != null)
            {
                con.ConnectionClosed -= OnConnectionClosed;
                con.CommandRecieved -= OnCommandRecieved;
                if (con.IsConnected) con.Close();
            }
            isConnected = false;
            ExistingUsers = new Dictionary<string, User>();
            existingChannels = new Dictionary<string, ExistingChannel>();
            joinedChannels = new Dictionary<string, Channel>();
            existingBattles = new Dictionary<int, Battle>();
            MyBattle = null;
            MyBattleID = 0;
            username = "";
            isLoggedIn = false;
            isConnected = false;
            isChanScanning = false;
            if (guiThreadInvoker != null) guiThreadInvoker(() => ConnectionLost(this, new TasEventArgs("Connection was closed")));
            else ConnectionLost(this, new TasEventArgs("Connection was closed"));
        }

        public void EnableAllUnits()
        {
            MyBattle.DisabledUnits.Clear();
            con.SendCommand("ENABLEALLUNITS");
            BattleDisabledUnitsChanged(this, new TasEventArgs(MyBattle.DisabledUnits.ToArray()));
        }

        public void ForceAlly(string username, int ally)
        {
            con.SendCommand("FORCEALLYNO", username, ally);
        }

        public void ForceColor(string username, int color)
        {
            con.SendCommand("FORCETEAMCOLOR", username, color);
        }

        public void ForceSide(string user, int side)
        {
            var ubs = MyBattle.Users.SingleOrDefault(u => u.Name == user);
            if (ubs != null)
            {
                ubs.Side = side;
                con.SendCommand("FORGEMSG", user, "CLIENTBATTLESTATUS", ubs.ToInt(), ubs.TeamColor);
                con.SendCommand("FORGEREVERSEMSG", user, "MYBATTLESTATUS", ubs.ToInt(), ubs.TeamColor);
            }
        }

        public void ForceSpectator(string username)
        {
            con.SendCommand("FORCESPECTATORMODE", username);
        }

        public void ForceTeam(string username, int team)
        {
            con.SendCommand("FORCETEAMNO", username, team);
        }

        public void GameSaid(string username, string text)
        {
            InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Player, TasSayEventArgs.Places.Game, "", username, text, false));
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


        public void JoinBattle(int battleID, string password = "*")
        {
            if (string.IsNullOrEmpty(password)) password = "*";
            con.SendCommand("JOINBATTLE", battleID, password, random.Next());
        }


        public void JoinChannel(string channelName, string key=null)
        {
            if (!String.IsNullOrEmpty(key)) con.SendCommand("JOIN", channelName, key);
            else con.SendCommand("JOIN", channelName);
        }

        public void Kick(string username)
        {
            con.SendCommand("KICKFROMBATTLE", username);
        }

        public void AdminKickFromLobby(string username,string reason)
        {
            con.SendCommand("KICKUSER", username,reason);
        }

        public void AdminSetTopic(string channel, string topic) {
            Say(SayPlace.User, "ChanServ", string.Format("!topic #{0} {1}", channel, topic.Replace("\n","\\n")),false);
        }

        public void AdminSetChannelPassword(string channel, string password) {
            if (string.IsNullOrEmpty(password) || password=="*") {
                Say(SayPlace.User, "ChanServ",string.Format("!lock #{0} {1}", channel, password),false);    
            } else {
                Say(SayPlace.User, "ChanServ", string.Format("!unlock #{0}", channel), false);
            }
        }

        public void LeaveBattle()
        {
            if (MyBattle != null)
            {
                con.SendCommand("LEAVEBATTLE");
                var bat = MyBattle;
                bat.ScriptTags.Clear();
                MyBattle = null;
                MyBattleID = 0;
                BattleClosed(this, new EventArgs<Battle>(bat));
            }
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
            var nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault();
            string data = "0";
            if (nic != null) {
                data = string.Join(":", nic.GetPhysicalAddress().GetAddressBytes()) + "lobby.springrts.com";
            }
            return Crc.Crc32(Encoding.ASCII.GetBytes(data)).ToString();
        }


        public void Login(string userName, string password)
        {
            if (con == null) throw new TasClientException("Not connected");

            UserPassword = password;

            con.SendCommand("LOGIN", userName, Utils.HashLobbyPassword(password), cpu, localIp, appName, "\t" + GetMyUserID(), "\ta sp eb");
        }

        int cpu = Environment.OSVersion.Platform == PlatformID.Unix ? GlobalConst.ZkLobbyUserCpuLinux : GlobalConst.ZkLobbyUserCpu;

        public void OpenBattle(Battle nbattle)
        {
            LeaveBattle(); // leave current battle
            MyBattleID = -1;

            MyBattle = nbattle;

            var objList = new List<object>
                          {
                              0,
                              // type normal
                              (int)MyBattle.Nat,
                              MyBattle.Password,
                              MyBattle.HostPort,
                              MyBattle.MaxPlayers,
                              MyBattle.ModHash,
                              MyBattle.Rank,
                              MyBattle.MapHash,
                              MyBattle.EngineName,
                              MyBattle.EngineVersion,
                              MyBattle.MapName,
                              '\t' + MyBattle.Title,
                              '\t' + MyBattle.ModName
                          };
            MyBattle.Founder = ExistingUsers[username];
            MyBattle.Ip = localIp;

            //battle.Details.AddToParamList(objList);
            mapToChangeTo = MyBattle.MapName;
            mapChecksumToChangeTo = MyBattle.MapHash ?? 0;
            lockToChangeTo = false;

            con.SendCommand("OPENBATTLEEX", objList.ToArray());

            lastSpectatorCount = 0;

            // send predefined starting rectangles
            foreach (var v in MyBattle.Rectangles) con.SendCommand("ADDSTARTRECT", v.Key, v.Value.Left, v.Value.Top, v.Value.Right, v.Value.Bottom);
        }




        public void Register(string username, string password)
        {
            con.SendCommand("REGISTER", username, Utils.HashLobbyPassword(password));
        }

        public void RemoveBattleRectangle(int allyno)
        {
            if (MyBattle.Rectangles.ContainsKey(allyno))
            {
                MyBattle.Rectangles.Remove(allyno);
                con.SendCommand("REMOVESTARTRECT", allyno);
            }
        }

        public void RemoveBot(string name)
        {
            con.SendCommand("REMOVEBOT", name);
        }


        public void RenameAccount(string newName)
        {
            con.SendCommand("RENAMEACCOUNT", newName);
        }

        public void Ring(string username)
        {
            con.SendCommand("RING", username);
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
            if (user != null && battle != null) {
                /*if (user.IsZkLobbyUser)
                {
                    Say(SayPlace.User, user.Name, "!join " + battle.Founder.Name, false);
                }
                else {*/
                    //con.SendCommand(string.Format("KICKFROMBATTLE {0}", name));
                    //con.SendCommand(string.Format("FORCEJOINBATTLE {0} {1} {2}", name, battleID, password));
                    con.SendCommand(string.Format("FORGEREVERSEMSG {0} LEAVEBATTLE", name));
                    con.SendCommand(string.Format("FORGEREVERSEMSG {0} JOINBATTLE {1}", name, battleID));
                //}
            }
        }

        public void ForceJoinChannel(string user, string channel, string password= null) {
            con.SendCommand(string.Format("FORCEJOIN {0} {1} {2}", user, channel, password));
            //if (string.IsNullOrEmpty(password)) con.SendCommand(string.Format("FORGEREVERSEMSG {0} JOIN {1}", user,channel));
            //else con.SendCommand(string.Format("FORGEREVERSEMSG {0} JOIN {1} {2}", user,channel,password));
        }

        public void ForceLeaveChannel(string user, string channel, string reason = null)
        {
            con.SendCommand(string.Format("FORCELEAVECHANNEL {0} {1} {2}", channel, user, reason));
            //con.SendCommand(string.Format("FORGEREVERSEMSG {0} LEAVE {1}", user, channel));
        }


        /// <summary>
        /// Say something through chat system
        /// </summary>
        /// <param Name="place">Pick user (private message) channel or battle</param>
        /// <param Name="channel">Channel or User Name</param>
        /// <param Name="inputtext">chat text</param>
        /// <param Name="isEmote">is message emote? (channel or battle only)</param>
        public void Say(SayPlace place, string channel, string inputtext, bool isEmote)
        {
            if (String.IsNullOrEmpty(inputtext)) return;
            var lines = inputtext.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var text in lines)
            {
                if (String.IsNullOrEmpty(text)) continue;

                var args = new SayingEventArgs(place, channel, text, isEmote);
                Saying(this, args);
                if (args.Cancel) continue;

                switch (place)
                {
                    case SayPlace.Channel:
                        if (!JoinedChannels.ContainsKey(args.Channel)) JoinChannel(args.Channel);
                        if (args.IsEmote) con.SendCommand("SAYEX", args.Channel, args.Text);
                        else con.SendCommand("SAY", args.Channel, args.Text);
                        break;

                    case SayPlace.User:
                        con.SendCommand("SAYPRIVATE", args.Channel, args.Text);
                        break;

                    case SayPlace.Battle:
                        if (args.IsEmote) con.SendCommand("SAYBATTLEEX", args.Text);
                        else con.SendCommand("SAYBATTLE", args.Text);
                        break;
                    case SayPlace.BattlePrivate:
                        if (args.IsEmote) con.SendCommand("SAYBATTLEPRIVATEEX", channel, args.Text);
                        else con.SendCommand("SAYBATTLEPRIVATE", channel, args.Text);
                        break;
                }
            }
        }


        public void SendMyBattleStatus(UserBattleStatus status)
        {
            con.SendCommand("MYBATTLESTATUS", status.ToInt(), status.TeamColor);
        }

        public void SendMyBattleStatus(int battleStatus, int color)
        {
            con.SendCommand("MYBATTLESTATUS", battleStatus, color);
        }

        public void SendRaw(string text)
        {
            con.SendCommand("", text);
        }

        public void SetHideCountry(string name, bool state) {
            con.SendCommand("HIDECOUNTRYSET", name, state ? "1" : "0");
        }

        public void SetScriptTag(string data)
        {
            con.SendCommand("SETSCRIPTTAGS", data);
        }

        public void SetBotMode(string name, bool botMode) {
            con.SendCommand("SETBOTMODE",name, botMode?"1":"0");
        }

        public void RequestLobbyVersion(string name) {
            con.SendCommand("GETLOBBYVERSION", name); 
        }


        public void RequestUserIP(string name) {
            con.SendCommand("GETIP",name);
        }

        public void RequestUserID(string name)
        {
            con.SendCommand("GETUSERID", name);
        }


        /// <summary>
        /// Starts game and automatically does hole punching if necessary
        /// </summary>
        public void StartGame()
        {
            if (MyBattle.Nat == Battle.NatMode.HolePunching)
            {
                startingAfterUdpPunch = true;
                SendUdpPacket(0, serverHost, serverUdpHolePunchingPort);
                udpPunchingTimer.Start();
            }
            else ChangeMyUserStatus(false, true);
        }

        public void UpdateBattleDetails(BattleDetails bd)
        {
            var objList = new List<object>();
            con.SendCommand("SETSCRIPTTAGS", bd.GetParamList());
        }

        public void UpdateBot(string name, UserBattleStatus battleStatus, int teamColor)
        {
            con.SendCommand("UPDATEBOT", name, battleStatus.ToInt(), teamColor);
        }

        void DispatchServerCommand(string command, string[] args)
        {
            if (guiThreadInvoker != null) guiThreadInvoker(() => DispatchServerCommandOnGuiThread(command, args));
            else DispatchServerCommandOnGuiThread(command, args);
        }


        private void HandleSpecialServerMessages(string[] args) {
            var text = Utils.Glue(args, 0);
            var match = Regex.Match(text, "<([^>]+)> is using (.+)");
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                var version = match.Groups[2].Value.Trim();
                UserLobbyVersionRecieved(this, new UserLobbyVersionEventArgs() { Name = name, LobbyVersion = version});
            }
            else
            {
                match = Regex.Match(text, "<([^>]+)> is currently bound to (.+)");
                if (match.Success) {
                    var name = match.Groups[1].Value;
                    var ip = match.Groups[2].Value.Trim();
                    UserIPRecieved(this, new UserIPEventArgs() { Name = name, IP = ip});
                }
                else
                {
                    match = Regex.Match(text, "<([^>]+)> is (.+)");
                    if (match.Success)
                    {
                        var name = match.Groups[1].Value;
                        int id;
                        if (int.TryParse(match.Groups[2].Value.Trim(), out id)) UserIDRecieved(this, new UserIDEventArgs() { Name = name, ID = id });
                    }

                }
            }
            
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
                        serverVersion = args[0];
                        int.TryParse(args[2], out serverUdpHolePunchingPort);
                        ServerSpringVersion = args[1];
                        isConnected = true;
                        Connected(this, new TasEventArgs());
                        break;

                    case "ACCEPTED": // Login accepted
                        username = args[0];
                        isLoggedIn = true;
                        if (LoginAccepted != null) LoginAccepted(this, new TasEventArgs());
                        break;

                    case "DENIED": // login denied
                        isLoggedIn = false;
                        LoginDenied(this, new TasEventArgs(Utils.Glue(args)));
                        break;

                    case "JOIN": // channel joined
                    {
                        if (!JoinedChannels.ContainsKey(args[0])) {
                            JoinedChannels.Add(args[0], Channel.Create(args[0]));

                            var cancelEventArgs = new CancelEventArgs<TasEventArgs>(new TasEventArgs(args));
                            PreviewChannelJoined(this, cancelEventArgs);
                            if (!cancelEventArgs.Cancel) {
                                ChannelJoined(this, new TasEventArgs(args));
                            }
                        }
                        break;
                    }

                    case "JOINFAILED": // channel join failed
                        ChannelJoinFailed(this, new TasEventArgs(Utils.Glue(args)));
                        break;

                    case "CHANNEL": // iterating channels
                    {
                        var c = new ExistingChannel();
                        c.name = args[0];
                        int.TryParse(args[1], out c.userCount);
                        if (args.Length >= 3) c.topic = Utils.Glue(args, 2);
                        ExistingChannels.Add(c.name, c);
                    }
                        break;

                    case "ENDOFCHANNELS": // end of channel list iteration
                        isChanScanning = false;
                        ChannelListDone(this, new TasEventArgs());
                        break;

                    case "ADDUSER": // new user joined ta server
                    {
                        try
                        {
                            var u = User.Create(args[0]);
                            u.Country = args[1];
                            int cpu;
                            int.TryParse(args[2], out cpu);
                            u.Cpu = cpu;
                            u.LobbyID = Convert.ToInt32(args[3]);
                            ExistingUsers.Add(u.Name, u);
                            UserAdded(this, new EventArgs<User>(u));
                        }
                        catch (Exception e)
                        {
                            Trace.WriteLine("Error in processing ADDUSER: " + e);
                        }
                    }
                        break;

                    case "FORCELEAVECHANNEL":
                    {
                        var channel = args[0];
                        JoinedChannels.Remove(channel);
                        ChannelForceLeave(this, new TasEventArgs(args));
                        break;
                    }

                    case "FORCEQUITBATTLE":
                        BattleForceQuit(this, EventArgs.Empty);
                        break;

                    case "REMOVEUSER": // user left ta server
                    {
                        var userName = args[0];
                        var user = ExistingUsers[userName];
                        UserRemoved(this, new TasEventArgs(args));
                        ExistingUsers.Remove(userName);
                        break;
                    }

                    case "MOTD": // server motd
                        if (args.Length > 0)
                            InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Server,
                                                           TasSayEventArgs.Places.Motd,
                                                           "",
                                                           "",
                                                           Utils.Glue(args, 0),
                                                           false));
                        break;

                    case "SERVERMSG": // server message
                        HandleSpecialServerMessages(args);
                        InvokeSaid(new TasSayEventArgs(TasSayEventArgs.Origins.Server,
                                                       TasSayEventArgs.Places.Server,
                                                       "",
                                                       "",
                                                       Utils.Glue(args, 0),
                                                       false));
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

                    case "RING":

                        Rang(this, new EventArgs<string>(args[0]));

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
                        // removed due to bugs      
                        Redirect(this, new TasEventArgs(args));
                        break;

                    case "CLIENTSTATUS": // client's status changed
                    {
                        int status;
                        int.TryParse(args[1], out status);

                        var u = ExistingUsers[args[0]];
                        var old = u.Clone();
                        u.FromInt(status);

                        if (u.Name == username) lastUserStatus = u.ToInt();

                        if (u.IsInGame && old.IsInGame == false) BattleStarted(this, new EventArgs<User>(u));

                        if (MyBattle != null && MyBattle.Founder.Name == u.Name)
                        {
                            if (u.IsInGame && old.IsInGame == false) MyBattleStarted(this, new TasEventArgs(args));
                            if (!u.IsInGame && old.IsInGame == true) MyBattleHostExited(this, new TasEventArgs(args));
                        }

                        UserStatusChanged(this, new TasEventArgs(args));
                    }
                        break;

                    case "CLIENTS": // client list sent after channel join
                    {
                        var usrs = Utils.Glue(args, 1).Split(' ');
                        foreach (var s in usrs) JoinedChannels[args[0]].ChannelUsers.Add(s);
                        ChannelUsersAdded(this, new TasEventArgs(args));
                    }
                        break;

                    case "JOINED": // user joined one of my channels
                    {
                        var channelName = args[0];
                        var userName = args[1];
                        var channel = JoinedChannels[channelName];
                        channel.ChannelUsers.Add(userName);
                        ChannelUserAdded(this, new TasEventArgs(channelName, userName));
                    }
                        break;

                    case "LEFT": // user left one of my channels
                    {
                        var channelName = args[0];
                        var userName = args[1];
                        var reason = Utils.Glue(args, 2);
                        var channel = JoinedChannels[channelName];
                        channel.ChannelUsers.Remove(userName);
                        ChannelUserRemoved(this, new TasEventArgs(channelName, userName, reason));
                        break;
                    }

                    case "CHANNELTOPIC": // channel topic update (after joining a channel)
                    {
                        var c = JoinedChannels[args[0]];
                        c.TopicSetBy = args[1];
                        c.TopicSetDate = ConvertMilisecondTime(args[2]);
                        c.Topic = Utils.Glue(args, 3);
                        ChannelTopicChanged(this, new TasEventArgs(args[0]));
                    }
                        break;

                    case "OPENBATTLEFAILED": // opening new battle has failed
                        BattleOpenFailed(this, new TasEventArgs(Utils.Glue(args)));
                        break;

                    case "OPENBATTLE": // openbattle ok
                    {
                        MyBattleID = int.Parse(args[0]);
                        existingBattles[MyBattleID] = MyBattle;
                        var self = new UserBattleStatus(username, existingUsers[username]);
                        MyBattle.Users.Add(self); // add self
                        lastUserBattleStatus = self.ToInt();
                        UpdateBattleDetails(MyBattle.Details);
                        // SetScriptTag(MyBattle.Mod.GetDefaultModOptionsTags()); // sends default mod options // enable if tasclient is not fixed
                        BattleOpened(this, new TasEventArgs(args[0]));
                    }
                        break;

                    case "REQUESTBATTLESTATUS": // ask for status at the beginning of the battle
                        RequestBattleStatus(this, new EventArgs());
                        break;

                    case "JOINBATTLE": // we joined the battle
                    {
                        var joinedBattleID = Int32.Parse(args[0]);
                        MyBattleID = joinedBattleID;
                        var battle = existingBattles[joinedBattleID];
                        battle.Bots.Clear();
                        MyBattle = battle;
                        BattleJoined(this, new EventArgs<Battle>(MyBattle));
                    }
                        break;

                    case "JOINEDBATTLE": // user joined the battle
                    {
                        var joinedBattleID = Int32.Parse(args[0]);
                        Battle battle;
                        if (!existingBattles.TryGetValue(joinedBattleID, out battle)) break;
                        var userName = args[1];
                        var scriptPassword = args.Length > 2 ? args[2] : null;
                        var ubs = new UserBattleStatus(userName, existingUsers[userName], scriptPassword);
                        battle.Users.Add(ubs);
                        ExistingUsers[userName].IsInBattleRoom = true;
                        if (userName == username) lastUserBattleStatus = ubs.ToInt();
                        BattleUserJoined(this, new BattleUserEventArgs(userName, joinedBattleID, scriptPassword));
                    }
                        break;

                    case "JOINBATTLEFAILED": // user failed to join battle 
                    {
                        JoinBattleFailed(this, new TasEventArgs(args));
                    }
                        break;

                    case "ADDBOT": // bot added to battle
                        if (MyBattle != null && int.Parse(args[0]) == MyBattleID)
                        {
                            var bs = new BotBattleStatus(args[1], args[2], Utils.Glue(args, 5));
                            bs.SetFrom(int.Parse(args[3]), int.Parse(args[4]));
                            MyBattle.Bots.Add(bs);
                            BattleBotAdded(this, new EventArgs<BotBattleStatus>(bs));
                        }
                        break;

                    case "REMOVEBOT": // bot removed from battle
                        if (MyBattle != null && int.Parse(args[0]) == MyBattleID)
                        {
                            var toDel = MyBattle.Bots.Single(bot => bot.Name == args[1]);
                            MyBattle.Bots.Remove(toDel);
                            BattleBotRemoved(this, new EventArgs<BotBattleStatus>(toDel));
                        }
                        break;

                    case "UPDATEBOT": // bot data changed
                        if (MyBattle != null && int.Parse(args[0]) == MyBattleID)
                        {
                            var st = MyBattle.Bots.Single(bot => bot.Name == args[1]);
                            st.SetFrom(int.Parse(args[2]), int.Parse(args[3]));
                            BattleBotUpdated(this, new EventArgs<BotBattleStatus>(st));
                        }
                        break;

                    case "LEFTBATTLE": // user left the battle
                    {
                        var battleID = Int32.Parse(args[0]);
                        var user = args[1];
                        Battle battle;
                        if (!existingBattles.TryGetValue(battleID, out battle)) break;
                        if (!existingUsers.ContainsKey(user)) break;
                        battle.RemoveUser(user);
                        battle.ScriptTags.Clear();
                        var userName = args[1];
                        ExistingUsers[userName].IsInBattleRoom = false;

                        if (MyBattle != null && battleID == MyBattleID)
                        {
                            if (MyBattle.Founder.Name == UserName) UpdateSpectators();
                            if (user == username)
                            {
                                MyBattle = null;
                                MyBattleID = 0;
                                BattleClosed(this, new EventArgs<Battle>(battle));
                            }
                        }
                        BattleUserLeft(this, new BattleUserEventArgs(userName, battleID));
                    }
                        break;

                    case "CLIENTBATTLESTATUS": // player battle status has changed
                    {
                        var userIndex = MyBattle.GetUserIndex(args[0]);
                        if (userIndex != -1)
                        {
                            var battleStatus = MyBattle.Users[userIndex];
                            battleStatus.SetFrom(int.Parse(args[1]), int.Parse(args[2]));
                            MyBattle.Users[userIndex] = battleStatus;
                            if (MyBattle.Founder.Name == username) UpdateSpectators(); 
                            if (battleStatus.Name == username)
                            {
                                lastUserBattleStatus = battleStatus.ToInt();
                                BattleMyUserStatusChanged(this, new TasEventArgs(args));
                            }
                            BattleUserStatusChanged(this, new TasEventArgs(args));
                        }
                    }
                        break;

                    case "UPDATEBATTLEINFO": // update external battle info (lock and map)
                    {
                        var battleID = Int32.Parse(args[0]);
                        var specCount = Int32.Parse(args[1]);
                        var mapName = Utils.Glue(args, 4);
                        var mapHash = Int32.Parse(args[3]);
                        var isLocked = Int32.Parse(args[2]) > 0;

                        Battle battle;
                        if (!existingBattles.TryGetValue(battleID, out battle)) break;
                        battle.SpectatorCount = specCount;

                        var bi = new BattleInfoEventArgs(battleID, specCount, mapName, mapHash, isLocked);

                        if (battle.MapName != mapName || battle.MapHash != mapHash)
                        {
                            battle.MapName = mapName;
                            battle.MapHash = mapHash;
                            if (battle == MyBattle) MyBattleMapChanged(this, bi);
                            BattleMapChanged(this, bi);
                        }

                        if (battle.IsLocked != isLocked)
                        {
                            battle.IsLocked = isLocked;
                            BattleLockChanged(this, bi);
                        }
                        BattleInfoChanged(this, bi);
                    }
                        break;

                    case "BATTLEOPENEDEX":
                    {
                        var mapHash = args[9];
                        var rest = Utils.Glue(args, 12).Split('\t');
                        var mapName = rest[0];
                        var modName = rest[2];
                        if (!IsBattleVisible(modName)) break;

                        var newBattle = new Battle
                                        {
                                            BattleID = Int32.Parse(args[0]),
                                            IsReplay = args[1] != "0",
                                            Founder = ExistingUsers[args[3]],
                                            Ip = args[4],
                                            HostPort = Int32.Parse(args[5]),
                                            MaxPlayers = Int32.Parse(args[6]),
                                            Password = args[7] != "1" ? "*" : "apassword",
                                            Rank = Int32.Parse(args[8]),
                                            EngineVersion = args[11],
                                            EngineName = args[10]
                                            
                                        };

                        if (newBattle.Founder.Name == username) newBattle.Ip = localIp; // lobby can send wahtever, betteroverride here

                        // NatType = Int32.Parse(args[2]); // todo: correctly add nattype
                        newBattle.MapName = mapName;
                        newBattle.MapHash = Int32.Parse(mapHash);
                        newBattle.Title = rest[1];
                        newBattle.ModName = modName;
                        newBattle.Users.Add(new UserBattleStatus(newBattle.Founder.Name, newBattle.Founder));
                        existingBattles[newBattle.BattleID] = newBattle;
                        newBattle.Founder.IsInBattleRoom = true;
                        BattleFound(this, new EventArgs<Battle>(newBattle));
                        break;
                    }

                    case "BATTLECLOSED":
                    {
                        var battleID = Int32.Parse(args[0]);
                        Battle battle;
                        if (!existingBattles.TryGetValue(battleID, out battle)) break;
                        foreach (var u in battle.Users)
                        {
                            User user;
                            if (ExistingUsers.TryGetValue(u.Name, out user)) user.IsInBattleRoom = false;
                        }
                        if (battle == MyBattle)
                        {
                            battle.ScriptTags.Clear();
                            BattleClosed(this, new EventArgs<Battle>(battle));
                            MyBattleEnded(this, new EventArgs<Battle>(battle));
                        }
                        BattleEnding(this, new EventArgs<Battle>(battle));
                        existingBattles.Remove(battleID);
                        BattleEnded(this, new EventArgs<Battle>(battle));
                    }
                        break;

                    case "CLIENTIPPORT":
                    {
                        var idx = MyBattle.GetUserIndex(args[0]);
                        if (idx != -1)
                        {
                            var bs = MyBattle.Users[idx];
                            bs.ip = IPAddress.Parse(args[1]);
                            bs.port = int.Parse(args[2]);
                            MyBattle.Users[idx] = bs;
                            BattleUserIpRecieved(this, new TasEventArgs(args));
                        }
                    }
                        break;

                    case "SETSCRIPTTAGS": // updates internal battle details
                    {
                        var bd = new BattleDetails();
                        bd.Parse(Utils.Glue(args), MyBattle.ModOptions);
                        MyBattle.Details = bd;
                        MyBattle.ScriptTags.AddRange(args);
                        BattleDetailsChanged(this, new TasEventArgs(args));
                    }
                        break;

                    case "UDPSOURCEPORT":
                        udpPunchingTimer.Stop();
                        if (startingAfterUdpPunch)
                        {
                            startingAfterUdpPunch = false;

                            // send UDP packets to client (2x to be sure)
                            foreach (var ubs in MyBattle.Users) if (ubs.ip != IPAddress.None && ubs.port != 0) SendUdpPacket(lastUdpSourcePort, ubs.ip.ToString(), ubs.port);
                            foreach (var ubs in MyBattle.Users) if (ubs.ip != IPAddress.None && ubs.port != 0) SendUdpPacket(lastUdpSourcePort, ubs.ip.ToString(), ubs.port);

                            MyBattle.HostPort = lastUdpSourcePort; // update source port for hosting and start it
                            ChangeMyUserStatus(false, true);
                        }
                        break;

                    case "AGREEMENT":
                        if (agreementText == null) agreementText = new StringBuilder();
                        agreementText.AppendLine(Utils.Glue(args));
                        break;

                    case "AGREEMENTEND":
                        AgreementRecieved(this, new TasEventAgreementRecieved(agreementText));
                        agreementText = null;
                        break;

                    case "REGISTRATIONDENIED":
                        RegistrationDenied(this, new TasEventArgs(Utils.Glue(args)));
                        break;

                    case "REGISTRATIONACCEPTED":
                        RegistrationAccepted(this, new TasEventArgs(args));
                        break;

                    case "ACQUIREUSERID":
                        con.SendCommand("USERID", GetMyUserID());
                        break;
                    case "ADDSTARTRECT":
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
                        break;
                    case "REMOVESTARTRECT":
                    {
                        var allyNo = int.Parse(args[0]);
                        MyBattle.Rectangles.Remove(allyNo);
                        StartRectRemoved(this, new TasEventArgs(args));
                    }
                        break;
                    case "TESTLOGINACCEPT":
                        TestLoginAccepted(this, new TasEventArgs(args));
                        break;
                    case "TESTLOGINDENY":
                        TestLoginDenied(this, new TasEventArgs(args));
                        break;
                }
            }
            catch (Exception e)
            {
                var newEx =
                    new ApplicationException(string.Format("Error while processing command {0} {1}: {2}", command, Utils.Glue(args), e.Message), e);

                Trace.TraceError("TASC error {0}", newEx);
            }
        }

        void InvokeSaid(TasSayEventArgs sayArgs)
        {
            var previewSaidEventArgs = new CancelEventArgs<TasSayEventArgs>(sayArgs);
            PreviewSaid(this, previewSaidEventArgs);
            if (!previewSaidEventArgs.Cancel) Said(this, sayArgs);
        }

        bool IsBattleVisible(string modname)
        {
            if (FilterBattleByMod != null)
            {
                var e = new CancelEventArgs<string>(modname);
                e.Cancel = false;
                FilterBattleByMod(this, e);
                return !e.Cancel;
            }
            else return true;
        }


        void SendUdpPacket(int sourcePort, string targetIp, int targetPort)
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            var local = new IPEndPoint(IPAddress.Any, sourcePort);
            s.Bind(local);
            //s.ExclusiveAddressUse = false;
            lastUdpSourcePort = ((IPEndPoint)s.LocalEndPoint).Port;

            s.Connect(targetIp, targetPort);
            s.Send(Encoding.ASCII.GetBytes(UserName)); // todo: make async?
            s.Close();
        }


        void UpdateBattleInfo(bool lck, string mapname, int checksum)
        {
            {
                lockToChangeTo = lck;
                con.SendCommand("UPDATEBATTLEINFO", MyBattle.Users.Count(user => user.IsSpectator), (lck ? 1 : 0), checksum, mapname);
            }
        }


        void UpdateSpectators()
        {
            {
                var n = MyBattle.Users.Count(x => x.IsSpectator);
                if (n != lastSpectatorCount)
                {
                    lastSpectatorCount = n;
                    con.SendCommand("UPDATEBATTLEINFO", n, lockToChangeTo ? 1 : 0, mapChecksumToChangeTo,  mapToChangeTo);
                }
            }
        }

        void OnCommandRecieved(object sender, ConnectionEventArgs args)
        {
            if (sender == con) DispatchServerCommand(args.Command, args.Parameters);
        }

        void OnConnectionClosed(object sender, EventArgs args)
        {
            ConnectionFailed = true;
            Disconnect();
        }

        void OnPingTimer(object sender, EventArgs args)
        {
            if (isConnected && con != null) con.SendCommand("PING");
            else if (!isConnected && ConnectionFailed)
            {
                ConnectionFailed = false;
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

        void udpPunchingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SendUdpPacket(lastUdpSourcePort, serverHost, serverUdpHolePunchingPort);
        }

        public void ChangePassword(string old, string newPass) {
            con.SendCommand("CHANGEPASSWORD", Utils.HashLobbyPassword(old), Utils.HashLobbyPassword(newPass));
        }
    }
}