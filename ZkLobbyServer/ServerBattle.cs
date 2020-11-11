using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using PlasmaDownloader;
using PlasmaShared;
using Ratings;
using ZeroKWeb.SpringieInterface;
using ZkData;
using ZkData.UnitSyncLib;
using static System.String;
using Timer = System.Timers.Timer;
 
namespace ZkLobbyServer
{
    public class ServerBattle : Battle
    {
        public const int PollTimeout = 60;
        public const int MapVoteTime = 25;
        public const int NumberOfMapChoices = 4;
        public const int MinimumAutostartPlayers = 6;
        public static int BattleCounter;
        public int QueueCounter = 0;

        public static readonly Dictionary<string, BattleCommand> Commands = new Dictionary<string, BattleCommand>();


        private static object pickPortLock = new object();
        private static string hostingIp;


        public int DiscussionSeconds = 25;
        public readonly List<string> toNotify = new List<string>();
        public Resource HostedMap;

        public Resource HostedMod;

        public Mod HostedModInfo;

        private int hostingPort;
        private int? dbAutohostIndex;

        protected bool isZombie;
        protected bool IsPollsBlocked => IsAutohost && DateTime.UtcNow < BlockPollsUntil;

        private List<KickedPlayer> kickedPlayers = new List<KickedPlayer>();
        public List<BattleDebriefing> Debriefings { get; private set; } = new List<BattleDebriefing>();

        private Timer pollTimer;
        private Timer discussionTimer;

        public ZkLobbyServer server;
        public DedicatedServer spring;
        public string battleInstanceGuid;
        PlayerTeam startGameStatus;

        public int InviteMMPlayers { get; protected set; } = int.MaxValue; //will invite players to MM after each battle if more than X players

        public MapSupportLevel MinimalMapSupportLevel => IsAutohost ? MinimalMapSupportLevelAutohost : (IsPassworded ? MapSupportLevel.Unsupported : MapSupportLevel.Supported);

        public CommandPoll ActivePoll { get; private set; }

        public bool IsAutohost { get; private set; }
        public bool IsDefaultGame { get; private set; } = true;
        public bool IsCbalEnabled { get; private set; } = true;

        public override bool TimeQueueEnabled => DynamicConfig.Instance.TimeQueueEnabled && (Mode == AutohostMode.Teams || Mode == AutohostMode.Game1v1 || Mode == AutohostMode.GameFFA);

        public MapSupportLevel MinimalMapSupportLevelAutohost { get; protected set; } = MapSupportLevel.Featured;


        static ServerBattle()
        {
            Commands =
                Assembly.GetAssembly(typeof(BattleCommand))
                    .GetTypes()
                    .Where(x => !x.IsAbstract && x.IsClass && typeof(BattleCommand).IsAssignableFrom(x))
                    .Select(x => x.GetConstructor(new Type[] { }).Invoke(new object[] { }))
                    .Cast<BattleCommand>()
                    .ToDictionary(x => x.Shortcut, x => x);

            hostingIp =
                Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ??
                "127.0.0.1";
        }

        public ServerBattle(ZkLobbyServer server, string founder)
        {
            BattleID = Interlocked.Increment(ref BattleCounter);
            FounderName = founder;
            battleInstanceGuid = Guid.NewGuid().ToString();

            this.server = server;
            pollTimer = new Timer(PollTimeout * 1000);
            pollTimer.Enabled = false;
            pollTimer.AutoReset = false;
            pollTimer.Elapsed += pollTimer_Elapsed;
            discussionTimer = new Timer(DiscussionSeconds * 1000);
            discussionTimer.Enabled = false;
            discussionTimer.AutoReset = false;
            discussionTimer.Elapsed += discussionTimer_Elapsed;
            SetupSpring();
            PickHostingPort();
        }

        public void SaveToDb()
        {
            if (!IsAutohost) return;
            using (var db = new ZkDataContext())
            {
                Autohost autohost = null;
                bool insert = false;
                if (dbAutohostIndex.HasValue)
                {
                    autohost = db.Autohosts.Where(x => x.AutohostID == dbAutohostIndex).FirstOrDefault();
                }
                if (autohost == null)
                {
                    insert = true;
                    autohost = new Autohost();
                }
                autohost.MinimumMapSupportLevel = MinimalMapSupportLevelAutohost;
                autohost.AutohostMode = Mode;
                autohost.InviteMMPlayers = InviteMMPlayers;
                autohost.MaxElo = MaxElo;
                autohost.MinElo = MinElo;
                autohost.MaxLevel = MaxLevel;
                autohost.MinLevel = MinLevel;
                autohost.MaxRank = MaxRank;
                autohost.MinRank = MinRank;
                autohost.Title = Title;
                autohost.MaxPlayers = MaxPlayers;
                autohost.CbalEnabled = IsCbalEnabled;
                autohost.MaxEvenPlayers = MaxEvenPlayers;
                autohost.ApplicableRating = ApplicableRating;
                if (insert)
                {
                    db.Autohosts.Add(autohost);
                }
                db.SaveChanges();
                dbAutohostIndex = autohost.AutohostID;
            }
        }

        public string GenerateClientScriptPassword(string name)
        {
            return Hash.HashString(battleInstanceGuid + name).ToString();
        }

        public void Dispose()
        {
            spring.UnsubscribeEvents(this);
            if (pollTimer != null) pollTimer.Enabled = false;
            pollTimer?.Dispose();
            pollTimer = null;
            if (discussionTimer != null) discussionTimer.Enabled = false;
            discussionTimer?.Dispose();
            discussionTimer = null;
            spring = null;
            ActivePoll = null;
        }

        public List<string> GetAllUserNames()
        {
            var ret = Users.Select(x => x.Key).ToList();
            if (spring.IsRunning) ret.AddRange(spring.Context.ActualPlayers.Select(x => x.Name));
            return ret.Distinct().ToList();
        }

        public BattleCommand GetCommandByName(string name)
        {
            BattleCommand command;
            if (Commands.TryGetValue(name, out command)) return command.Create();
            return null;
        }

        public ConnectSpring GetConnectSpringStructure(string scriptPassword, bool isSpectator)
        {
            return new ConnectSpring()
            {
                Engine = EngineVersion,
                Ip = hostingIp,
                Port = hostingPort,
                Map = MapName,
                Game = ModName,
                ScriptPassword = scriptPassword,
                Mode = Mode,
                Title = Title,
                IsSpectator = isSpectator,
            };
        }

        public bool IsKicked(string name)
        {
            var kicked = false;
            kickedPlayers.RemoveAll(x => x.TimeOfKicked <= DateTime.UtcNow.AddMinutes(-5));
            if (kickedPlayers.Any(y => y.Name == name)) kicked = true;
            return kicked;
        }


        public async Task KickFromBattle(string name, string reason)
        {
            UserBattleStatus user;
            kickedPlayers.Add(new KickedPlayer() { Name = name });
            if (Users.TryGetValue(name, out user))
            {
                var client = server.ConnectedUsers[name];
                await client.Respond($"You were kicked from battle: {reason}");
                await client.Process(new LeaveBattle() { BattleID = BattleID });
            }
        }

        public virtual async Task CheckCloseBattle()
        {
            if (Users.IsEmpty && !spring.IsRunning)
            {
                if (!IsAutohost)
                    await server.RemoveBattle(this);
                else if (Mode != AutohostMode.None) // custom autohosts would typically be themed around a single map
                    await RunCommandDirectly<CmdMap>(null);
            }
        }

        public void SwitchDefaultGame(bool useDefaultGame)
        {
            IsDefaultGame = useDefaultGame;
        }

        public void SwitchAutohost(bool autohost, string founder)
        {
            if (autohost)
            {
                IsAutohost = true;
                IsDefaultGame = true;
                FounderName = "Autohost #" + BattleID;
                SaveToDb();
            }
            else
            {
                IsAutohost = false;
                FounderName = founder;
                if (dbAutohostIndex.HasValue)
                {
                    using (var db = new ZkDataContext())
                    {
                        db.Autohosts.Remove(db.Autohosts.Where(x => x.AutohostID == dbAutohostIndex).FirstOrDefault());
                        db.SaveChanges();
                    }
                }
            }
        }

        public async Task ProcessBattleSay(Say say)
        {
            if (say.User == GlobalConst.NightwatchName) return; // ignore self

            ConnectedUser user;
            server.ConnectedUsers.TryGetValue(say.User, out user);
            if ((say.Place == SayPlace.Battle) && !say.IsEmote && (user?.User.BanMute != true) && (user?.User.BanSpecChat != true) && say.AllowRelay) spring.SayGame($"<{say.User}>{say.Text}"); // relay to spring

            await CheckSayForCommand(say);
        }

        private async Task<bool> CheckSayForCommand(Say say)
        {
            // check if it's command
            if (!say.IsEmote && (say.Text?.Length > 1) && say.Text.StartsWith("!"))
            {
                var parts = say.Text.Substring(1).Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                return await RunCommandWithPermissionCheck(say, parts[0], parts.Skip(1).FirstOrDefault());
            }
            return false;
        }

        public virtual async Task ProcessPlayerJoin(ConnectedUser user, string joinPassword)
        {
            if (IsPassworded && (Password != joinPassword))
            {
                await user.Respond("Invalid password");
                return;
            }

            if (IsKicked(user.Name))
            {
                await KickFromBattle(user.Name, "Banned for five minutes");
                return;
            }

            if ((user.MyBattle != null) && (user.MyBattle != this)) await user.Process(new LeaveBattle());

            UserBattleStatus ubs;
            if (!Users.TryGetValue(user.Name, out ubs))
            {
                ubs = new UserBattleStatus(user.Name, user.User, GenerateClientScriptPassword(user.Name));
                Users[user.Name] = ubs;
            }

            ValidateBattleStatus(ubs);
            user.MyBattle = this;


            await server.TwoWaySyncUsers(user.Name, Users.Keys); // mutually sync user statuses

            await server.SyncUserToAll(user);

            await RecalcSpectators();

            await
                user.SendCommand(new JoinBattleSuccess()
                {
                    BattleID = BattleID,
                    Players = Users.Values.Select(x => x.ToUpdateBattleStatus()).ToList(),
                    Bots = Bots.Values.Select(x => x.ToUpdateBotStatus()).ToList(),
                    Options = ModOptions
                });

            if (ActivePoll != null) await user.SendCommand(ActivePoll.GetBattlePoll());

            await server.Broadcast(Users.Keys.Where(x => x != user.Name), ubs.ToUpdateBattleStatus()); // send my UBS to others in battle

            if (spring.IsRunning)
            {
                spring.AddUser(ubs.Name, ubs.ScriptPassword, ubs.LobbyUser);
                var started = DateTime.UtcNow.Subtract(spring.IngameStartTime ?? RunningSince ?? DateTime.UtcNow);
                started = new TimeSpan((int)started.TotalHours, started.Minutes, started.Seconds);
                await SayBattle($"THIS GAME IS CURRENTLY IN PROGRESS, PLEASE WAIT UNTIL IT ENDS! Running for {started}", ubs.Name);
                await SayBattle("If you say !notify, I will message you when the current game ends.", ubs.Name);
            }

            try
            {
                var ret = PlayerJoinHandler.AutohostPlayerJoined(GetContext(), ubs.LobbyUser.AccountID);
                if (ret != null)
                {
                    if (!IsNullOrEmpty(ret.PrivateMessage)) await SayBattle(ret.PrivateMessage, ubs.Name);
                    if (!IsNullOrEmpty(ret.PublicMessage)) await SayBattle(ret.PublicMessage);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                await SayBattle("ServerManage error: " + ex);
            }
        }


        public async Task RecalcSpectators()
        {
            var specCount = Users.Values.Count(x => x.IsSpectator);
            var playerCount = Users.Values.Count(x => !x.IsSpectator);
            if (specCount != SpectatorCount || playerCount != NonSpectatorCount)
            {
                SpectatorCount = specCount;
                NonSpectatorCount = playerCount;
                if (GlobalConst.LobbyServerUpdateSpectatorsInstantly)
                {
                    await server.Broadcast(Users.Keys, new BattleUpdate() { Header = new BattleHeader() { SpectatorCount = specCount, BattleID = BattleID, PlayerCount = NonSpectatorCount } });
                }
            }
        }


        public async Task RegisterVote(Say e, int vote)
        {
            if (ActivePoll != null)
            {
                if (await ActivePoll.Vote(e, vote))
                {
                    StopVote();
                }
            }
            else await Respond(e, "There is no poll going on, start some first");
        }

        public async Task RequestConnectSpring(ConnectedUser conus, string joinPassword)
        {
            UserBattleStatus ubs;

            startGameStatus = spring.LobbyStartContext.Players.FirstOrDefault(x => x.Name == conus.Name);
            
            if (!Users.TryGetValue(conus.Name, out ubs) && !(IsInGame && startGameStatus != null))
                if (IsPassworded && (Password != joinPassword))
                {
                    await conus.Respond("Invalid password");
                    return;
                }
            var pwd = GenerateClientScriptPassword(conus.Name);
            spring.AddUser(conus.Name, pwd, conus.User);

            if (spring.Context.LobbyStartContext.Players.Any(x => x.Name == conus.Name) && conus.MyBattle != this)
            {
                await ProcessPlayerJoin(conus, joinPassword);
            }

            await conus.SendCommand(GetConnectSpringStructure(pwd, startGameStatus?.IsSpectator != false));
        }


        public Task Respond(Say e, string text)
        {
            return SayBattle(text, e?.User);
        }


        public async Task RunCommandDirectly<T>(Say e, string args = null) where T : BattleCommand, new()
        {
            var t = new T();
            await t.Run(this, e, args);
        }


        public async Task<bool> RunCommandWithPermissionCheck(Say e, string com, string arg)
        {
            var cmd = GetCommandByName(com);
            if (cmd == null) return false;
            if (isZombie)
            {
                await Respond(e, "This room is now disabled, please join a new one");
                return false;
            }
            string reason;
            var perm = cmd.GetRunPermissions(this, e.User, out reason);

            if (perm == BattleCommand.RunPermission.Run) await cmd.Run(this, e, arg);
            else if (perm == BattleCommand.RunPermission.Vote)
            {

                if (IsPollsBlocked)
                {
                    await Respond(e, "Please wait for a few seconds before starting a poll.");
                    return false;
                }
                await StartVote(cmd, e, arg);
            }
            else
            {
                await Respond(e, reason);
                return false;
            }
            return true;
        }


        public async Task<bool> RunServerBalance(bool isGameStart, int? allyTeams, bool? clanWise)
        {
            try
            {
                var context = GetContext();
                context.Mode = Mode;
                if (!IsCbalEnabled) clanWise = false;
                var balance = Balancer.BalanceTeams(context, isGameStart, allyTeams, clanWise);
                await ApplyBalanceResults(balance);
                return balance.CanStart;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return false;
            }
        }

        public void SayGame(string text)
        {
            if (spring?.IsRunning != true) return;
            foreach (var line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                spring.SayGame(line);
            }
        }

        public async Task SayBattle(string text, string privateUser = null)
        {
            if (!IsNullOrEmpty(text))
            {
                if ((privateUser == null)) spring.SayGame(text);
                foreach (var line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    await
                        server.GhostSay(
                            new Say()
                            {
                                User = GlobalConst.NightwatchName,
                                Text = line,
                                Place = privateUser != null ? SayPlace.BattlePrivate : SayPlace.Battle,
                                Target = privateUser,
                                IsEmote = true,
                                AllowRelay = false,
                            },
                            BattleID);
                }
            }
        }

        public async Task SetModOptions(Dictionary<string, string> options)
        {
            ModOptions = options;
            await server.Broadcast(Users.Keys, new SetModOptions() { Options = options });
        }
        public void SetApplicableRating(RatingCategory rating)
        {
            ApplicableRating = rating;
            SaveToDb();
        }


        public async Task Spectate(string name)
        {
            ConnectedUser usr;
            if (server.ConnectedUsers.TryGetValue(name, out usr)) await usr.Process(new UpdateUserBattleStatus() { Name = usr.Name, IsSpectator = true });
        }


        public async Task<bool> StartGame()
        {
            var context = GetContext();

            if (TimeQueueEnabled) // spectate beyond max players
            {
                int allowedPlayers = MaxPlayers;
                if (context.Players.Count <= MaxEvenPlayers)
                {
                    allowedPlayers = context.Players.Where(x => !x.IsSpectator).Count() & ~0x1;
                }
                foreach (var plr in context.Players.Where(x=>!x.IsSpectator).OrderBy(x => x.QueueOrder).Skip(allowedPlayers))
                {
                    plr.IsSpectator = true;
                }
            }
            
            
            if (Mode != AutohostMode.None)
            {
                var balance = IsCbalEnabled ? Balancer.BalanceTeams(context, true, null, null) : Balancer.BalanceTeams(context, true, null, false);

                if (!IsNullOrEmpty(balance.Message)) await SayBattle(balance.Message);
                if (!balance.CanStart) return false;
                context.ApplyBalance(balance);
            }

            var startSetup = StartSetup.GetDedicatedServerStartSetup(context);

            if (!await EnsureEngineIsPresent()) return false;

            if (IsInGame || spring.IsRunning)
            {
                await SayBattle("Game already running");
                return false;
            }
            spring.HostGame(startSetup, hostingIp, hostingPort);
            IsInGame = true;
            RunningSince = DateTime.UtcNow;
            foreach (var us in Users.Values)
                if (us != null)
                {
                    ConnectedUser user;
                    if (server.ConnectedUsers.TryGetValue(us.Name, out user)) await user.SendCommand(GetConnectSpringStructure(us.ScriptPassword, startSetup?.Players.FirstOrDefault(x=>x.Name == us.Name)?.IsSpectator != false));
                }
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = GetHeader() });

            // remove all from MM
            foreach (var player in startSetup.Players.Where(x => !x.IsSpectator)) {
                if (await server.MatchMaker.RemoveUser(player.Name, false))
                {
                    await server.UserLogSay($"Removing {player.Name} from MM since their custom battle just started.");
                }
            }
            await server.MatchMaker.UpdateAllPlayerStatuses();
            return true;
        }

        public async Task<bool> StartVote(BattleCommand cmd, Say e, string args, int timeout = PollTimeout, CommandPoll poll = null)
        {
            cmd = cmd.Create();
            string topic = cmd.Arm(this, e, args);
            if (topic == null) return false;

            var unwrappedCmd = cmd;
            if (cmd is CmdPoll)
            {
                var split = args.Split(new[] { ' ' }, 2);
                args = split.Length > 1 ? split[1] : "";
                unwrappedCmd = (cmd as CmdPoll).InternalCommand;
            }

            if (unwrappedCmd is CmdMap && string.IsNullOrEmpty(args)) return await CreateMultiMapPoll();

            Func<string, string> selector = cmd.GetIneligibilityReasonFunc(this);
            if (e != null && selector(e.User) != null) return false;
            var options = new List<PollOption>();

            string url = null;
            string map = null;
            if (unwrappedCmd is CmdMap)
            {
                url = $"{GlobalConst.BaseSiteUrl}/Maps/Detail/{(unwrappedCmd as CmdMap).Map.ResourceID}";
                map = (unwrappedCmd as CmdMap).Map.InternalName;
            }
            poll = poll ?? new CommandPoll(this, true, true, unwrappedCmd is CmdMap, map, unwrappedCmd is CmdStart);
            options.Add(new PollOption()
            {
                Name = "Yes",
                URL = url,
                Action = async () =>
                {
                    if (cmd.Access == BattleCommand.AccessType.NotIngame && spring.IsRunning) return;
                    if (cmd.Access == BattleCommand.AccessType.Ingame && !spring.IsRunning) return;
                    await cmd.ExecuteArmed(this, e);
                }
            });
            options.Add(new PollOption()
            {
                Name = "No",
                Action = async () => { }
            });

            if (await StartVote(selector, options, e, topic, poll))
            {
                await RegisterVote(e, 1);
                return true;
            }
            return false;
        }

        public async Task<bool> StartVote(Func<string, string> eligibilitySelector, List<PollOption> options, Say creator, string topic, CommandPoll poll, int timeout = PollTimeout)
        {
            if (ActivePoll != null)
            {
                await Respond(creator, $"Please wait, another poll already in progress: {ActivePoll.Topic}");
                return false;
            }
            await poll.Setup(eligibilitySelector, options, creator, topic);
            ActivePoll = poll;
            pollTimer.Interval = timeout * 1000;
            pollTimer.Enabled = true;
            return true;
        }


        public async void StopVote()
        {
            try
            {
                if (ActivePoll == null) return;
                var oldPoll = ActivePoll;
                if (ActivePoll != null) await ActivePoll.End();
                if (pollTimer != null) pollTimer.Enabled = false;
                ActivePoll = null;
                await oldPoll?.PublishResult();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error stopping vote " + ex);
            }
        }

        public async Task SwitchEngine(string engine)
        {
            EngineVersion = engine;
            ValidateAndFillDetails();
            await
                server.Broadcast(server.ConnectedUsers.Values,
                    new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Engine = EngineVersion } });
        }

        public async Task SwitchGame(string internalName)
        {
            ModName = internalName;
            ValidateAndFillDetails();
            await
                server.Broadcast(server.ConnectedUsers.Values,
                    new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Game = ModName } });
        }

        public async Task SwitchGameType(AutohostMode type)
        {
            Mode = type;
            MapName = null;
            ValidateAndFillDetails();
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = GetHeader() });
            SaveToDb();
            // do a full update - mode can also change map/players
        }

        public async Task SwitchMap(string internalName)
        {
            MapName = internalName;
            ValidateAndFillDetails();
            await
                server.Broadcast(server.ConnectedUsers.Values,
                    new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Map = MapName } });
        }

        public async Task SwitchMaxPlayers(int cnt)
        {
            MaxPlayers = cnt;
            ValidateAndFillDetails();
            await
                server.Broadcast(server.ConnectedUsers.Values,
                    new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, MaxPlayers = MaxPlayers } });
            SaveToDb();
        }
        public async Task SwitchMaxEvenPlayers(int cnt)
        {
            MaxEvenPlayers = cnt;
            ValidateAndFillDetails();
            await
                server.Broadcast(server.ConnectedUsers.Values,
                    new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, MaxEvenPlayers = MaxEvenPlayers } });
            SaveToDb();
        }
        public async Task SwitchInviteMmPlayers(int players)
        {
            InviteMMPlayers = players;
            SaveToDb();
        }

        public async Task ValidateAllBattleStatuses()
        {
            foreach (var ubs in Users.Values)
            {
                ValidateBattleStatus(ubs);
                await server.Broadcast(Users.Keys, ubs.ToUpdateBattleStatus());
            }
            await RecalcSpectators();
        }

        public async Task SwitchMaxElo(int elo)
        {
            MaxElo = elo;
            SaveToDb();
            await ValidateAllBattleStatuses();
        }

        public async Task SwitchMinElo(int elo)
        {
            MinElo = elo;
            SaveToDb();
            await ValidateAllBattleStatuses();
        }

        public async Task SwitchMaxLevel(int lvl)
        {
            MaxLevel = lvl;
            SaveToDb();
            await ValidateAllBattleStatuses();
        }

        public async Task SwitchMinLevel(int lvl)
        {
            MinLevel = lvl;
            SaveToDb();
            await ValidateAllBattleStatuses();
        }

        public async Task SwitchMaxRank(int rank)
        {
            MaxRank = rank;
            SaveToDb();
            await ValidateAllBattleStatuses();
        }

        public async Task SwitchMinRank(int rank)
        {
            MinRank = rank;
            SaveToDb();
            await ValidateAllBattleStatuses();
        }

        public async Task SwitchMinMapSupportLevel(MapSupportLevel lvl)
        {
            MinimalMapSupportLevelAutohost = lvl;
            SaveToDb();
        }

        public void SwitchCbal(bool cbalEnabled)
        {
            IsCbalEnabled = cbalEnabled;
            SaveToDb();
        }

        public async Task SwitchPassword(string pwd)
        {
            Password = pwd ?? "";
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = GetHeader() });
            // do a full update to hide pwd properly
        }

        public async Task SwitchTitle(string title)
        {
            Title = title;
            ValidateAndFillDetails();
            await
                server.Broadcast(server.ConnectedUsers.Values,
                    new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Title = Title } });
            SaveToDb();
        }

        public void BlockPolls(int seconds)
        {
            var target = DateTime.UtcNow.AddSeconds(seconds);
            if (BlockPollsUntil < target) BlockPollsUntil = target;
        }


        public void UpdateWith(Autohost autohost)
        {
            IsAutohost = true;
            MinimalMapSupportLevelAutohost = autohost.MinimumMapSupportLevel;
            Mode = autohost.AutohostMode;
            InviteMMPlayers = autohost.InviteMMPlayers;
            MaxElo = autohost.MaxElo;
            MinElo = autohost.MinElo;
            MaxLevel = autohost.MaxLevel;
            MinLevel = autohost.MinLevel;
            MaxRank = autohost.MaxRank;
            MinRank = autohost.MinRank;
            Title = autohost.Title;
            MaxPlayers = autohost.MaxPlayers;
            IsCbalEnabled = autohost.CbalEnabled;
            dbAutohostIndex = autohost.AutohostID;
            MaxEvenPlayers = autohost.MaxEvenPlayers;
            ApplicableRating = autohost.ApplicableRating;
            FounderName = "Autohost #" + BattleID;
            ValidateAndFillDetails();

            RunCommandDirectly<CmdMap>(null);
        }

        public override void UpdateWith(BattleHeader h)
        {
            // following variables cannot be overriden in serverbattle
            h.BattleID = BattleID;
            h.Founder = FounderName;
            h.IsRunning = IsInGame;
            h.RunningSince = RunningSince;
            h.SpectatorCount = SpectatorCount;
            h.PlayerCount = NonSpectatorCount;
            h.IsMatchMaker = IsMatchMakerBattle;


            base.UpdateWith(h);

            ValidateAndFillDetails();
        }

        public void ValidateAndFillDetails()
        {
            if (IsNullOrEmpty(Title)) Title = $"{FounderName}'s game";
            if (IsNullOrEmpty(EngineVersion) || (Mode != AutohostMode.None)) EngineVersion = server.Engine;
            server.Downloader.GetResource(DownloadType.ENGINE, server.Engine);

            switch (Mode)
            {
                case AutohostMode.Game1v1:
                    MaxPlayers = 2;
                    break;
                case AutohostMode.Planetwars:
                    if (MaxPlayers < 2) MaxPlayers = 16;
                    break;
                case AutohostMode.GameChickens:
                    if (MaxPlayers < 2) MaxPlayers = 10;
                    break;
                case AutohostMode.GameFFA:
                    if (MaxPlayers < 3) MaxPlayers = 16;
                    break;
                case AutohostMode.Teams:
                    if (MaxPlayers < 4) MaxPlayers = 16;
                    break;
                case AutohostMode.None:
                    if (MaxPlayers == 0) MaxPlayers = 16;
                    break;
            }
            if (MaxPlayers > DynamicConfig.Instance.MaximumBattlePlayers && !IsAutohost) MaxPlayers = DynamicConfig.Instance.MaximumBattlePlayers;
            if (MaxEvenPlayers > MaxPlayers) MaxEvenPlayers = MaxPlayers;

            HostedMod = MapPicker.FindResources(ResourceType.Mod, ModName ?? server.Game ?? GlobalConst.DefaultZkTag).FirstOrDefault();
            HostedMap = MapName != null
                ? MapPicker.FindResources(ResourceType.Map, MapName).FirstOrDefault()
                : MapPicker.GetRecommendedMap(GetContext());

            ModName = HostedMod?.InternalName ?? ModName ?? server.Game ?? GlobalConst.DefaultZkTag;
            MapName = HostedMap?.InternalName ?? MapName ?? "Small_Divide-Remake-v04";

            if (HostedMod != null)
                try
                {
                    HostedModInfo = MetaDataCache.ServerGetMod(HostedMod.InternalName);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Error loading mod metadata for {0} : {1}", HostedMod.InternalName, ex);
                }
        }

        public virtual void ValidateBattleStatus(UserBattleStatus ubs)
        {
            if (Mode != AutohostMode.None) ubs.AllyNumber = 0;

            if (!ubs.IsSpectator)
            {
                if (!TimeQueueEnabled && Users.Values.Count(x => !x.IsSpectator) > MaxPlayers)
                {
                    ubs.IsSpectator = true;
                    SayBattle("This battle is full.", ubs.Name);
                }
                if (Users.Values.Count(x => !x.IsSpectator) <= DynamicConfig.Instance.MaximumStatLimitedBattlePlayers || IsAutohost)
                {
                    if (ubs.LobbyUser.EffectiveElo > MaxElo && ubs.LobbyUser.EffectiveMmElo > MaxElo)
                    {
                        ubs.IsSpectator = true;
                        SayBattle("Your rating (" + Math.Min(ubs.LobbyUser.EffectiveElo, ubs.LobbyUser.EffectiveMmElo) + ") is too high. The maximum rating to play in this battle is " + MaxElo + ".", ubs.Name);
                    }
                    if (ubs.LobbyUser.EffectiveElo < MinElo && ubs.LobbyUser.EffectiveMmElo < MinElo)
                    {
                        ubs.IsSpectator = true;
                        SayBattle("Your rating (" + Math.Max(ubs.LobbyUser.EffectiveElo, ubs.LobbyUser.EffectiveMmElo) + ") is too low. The minimum rating to play in this battle is " + MinElo + ".", ubs.Name);
                    }
                    if (ubs.LobbyUser.Level > MaxLevel)
                    {
                        ubs.IsSpectator = true;
                        SayBattle("Your level (" + ubs.LobbyUser.Level + ") is too high. The maximum level to play in this battle is " + MaxLevel + ".", ubs.Name);
                    }
                    if (ubs.LobbyUser.Level < MinLevel)
                    {
                        ubs.IsSpectator = true;
                        SayBattle("Your level (" + ubs.LobbyUser.Level + ") is too low. The minimum level to play in this battle is " + MinLevel + ".", ubs.Name);
                    }
                    if (ubs.LobbyUser.Rank > MaxRank)
                    {
                        ubs.IsSpectator = true;
                        SayBattle("Your Rank (" + Ranks.RankNames[ubs.LobbyUser.Rank] + ") is too high. The maximum Rank to play in this battle is " + Ranks.RankNames[MaxRank] + ".", ubs.Name);
                    }
                    if (ubs.LobbyUser.Rank < MinRank)
                    {
                        ubs.IsSpectator = true;
                        SayBattle("Your Rank (" + Ranks.RankNames[ubs.LobbyUser.Rank] + ") is too low. The minimum Rank to play in this battle is " + Ranks.RankNames[MinRank] + ".", ubs.Name);
                    }
                }
                if (ubs.QueueOrder <= 0) ubs.QueueOrder = ++QueueCounter;
            }
            else
            {
                ubs.QueueOrder = -1;
            }
        }


        protected virtual async Task OnDedicatedExited(SpringBattleContext springBattleContext)
        {
            StopVote();
            IsInGame = false;
            RunningSince = null;
            BlockPollsUntil = DateTime.UtcNow.AddSeconds(DiscussionSeconds);

            bool result = BattleResultHandler.SubmitSpringBattleResult(springBattleContext, server, (debriefing) =>
            {
                Debriefings.Add(debriefing);
                server.Broadcast(springBattleContext.ActualPlayers.Select(x => x.Name), debriefing);
                Trace.TraceInformation("Battle ended: Sent out debriefings for B" + debriefing.ServerBattleID);
            });

            await server.Broadcast(server.ConnectedUsers.Keys, new BattleUpdate() { Header = GetHeader() });

            foreach (var s in toNotify)
                await
                    server.GhostSay(new Say()
                    {
                        User = GlobalConst.NightwatchName,
                        Text = $"** {FounderName} 's {Title} just ended, join me! **",
                        Target = s,
                        IsEmote = true,
                        Place = SayPlace.User,
                        Ring = true,
                        AllowRelay = false
                    });

            toNotify.Clear();

            var playingEligibleUsers = server.MatchMaker.GetEligibleQuickJoinPlayers(Users.Values.Where(x => !x.LobbyUser.IsAway && !x.IsSpectator && x.Name != null).Select(x => server.ConnectedUsers[x.Name]).ToList());
            if (playingEligibleUsers.Count() >= InviteMMPlayers)
            { //Make sure there are enough eligible users for a battle to be likely to happen

                //put all users into MM queue to suggest battles
                var teamsQueues = server.MatchMaker.PossibleQueues.Where(x => x.Mode == AutohostMode.Teams).ToList();
                var availableUsers = Users.Values.Where(x => !x.LobbyUser.IsAway && x.Name != null).Select(x => server.ConnectedUsers[x.Name]).ToList();
                await server.MatchMaker.MassJoin(availableUsers, teamsQueues);
                DiscussionSeconds = MatchMaker.TimerSeconds + 2;
            }
            else
            {
                DiscussionSeconds = 5;
            }
            BlockPollsUntil = DateTime.UtcNow.AddSeconds(DiscussionSeconds);


            if (Mode != AutohostMode.None && (IsAutohost || (!Users.ContainsKey(FounderName) || Users[FounderName].LobbyUser?.IsAway == true) && Mode != AutohostMode.Planetwars && !IsPassworded))
            {
                if (!result)
                {
                    //Game was aborted/exited/invalid, allow manual commands
                    BlockPollsUntil = DateTime.UtcNow;
                }
                else
                {
                    //Initiate discussion time, then map vote, then start vote
                    discussionTimer.Interval = (DiscussionSeconds - 1) * 1000;
                    discussionTimer.Start();
                }
            }
            await CheckCloseBattle();
        }


        private async Task<bool> CreateMultiMapPoll()
        {

            var poll = new CommandPoll(this, false, false, true);
            poll.PollEnded += MapVoteEnded;
            var options = new List<PollOption>();
            List<int> pickedMaps = new List<int>();
            pickedMaps.Add(HostedMap?.ResourceID ?? 0);
            using (var db = new ZkDataContext())
            {
                for (int i = 0; i < NumberOfMapChoices; i++)
                {
                    Resource map = null;
                    if (i < NumberOfMapChoices / 2)
                    {
                        map = MapPicker.GetRecommendedMap(GetContext(), (MinimalMapSupportLevel < MapSupportLevel.Supported) ? MapSupportLevel.Supported : MinimalMapSupportLevel, MapRatings.GetMapRanking(Mode).TakeWhile(x => x.Percentile < 0.2).Select(x => x.Map).Where(x => !pickedMaps.Contains(x.ResourceID)).AsQueryable()); //choose at least 50% popular maps
                    }
                    if (map == null)
                    {
                        map = MapPicker.GetRecommendedMap(GetContext(), (MinimalMapSupportLevel < MapSupportLevel.Featured) ? MapSupportLevel.Supported : MinimalMapSupportLevel, db.Resources.Where(x => !pickedMaps.Contains(x.ResourceID)));
                    }
                    pickedMaps.Add(map.ResourceID);
                    options.Add(new PollOption()
                    {
                        Name = map.InternalName,
                        DisplayName = map.MapNameWithDimensions(),
                        URL = $"{GlobalConst.BaseSiteUrl}/Maps/Detail/{map.ResourceID}",
                        ResourceID = map.ResourceID,
                        Action = async () =>
                        {
                            var cmd = new CmdMap().Create();
                            cmd.Arm(this, null, map.ResourceID.ToString());
                            if (cmd.Access == BattleCommand.AccessType.NotIngame && spring.IsRunning) return;
                            if (cmd.Access == BattleCommand.AccessType.Ingame && !spring.IsRunning) return;
                            await cmd.ExecuteArmed(this, null);
                        }
                    });
                }
            }
            return await StartVote(new CmdMap().GetIneligibilityReasonFunc(this), options, null, "Choose the next map", poll, MapVoteTime);
        }

        private void discussionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                discussionTimer.Stop();
                CreateMultiMapPoll();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error creating map poll: " + ex);
            }
        }

        private void MapVoteEnded(object sender, PollOutcome e)
        {
            if (Users.Values.Count(x => !x.IsSpectator) >= MinimumAutostartPlayers) StartVote(new CmdStart(), null, "", MapVoteTime);
        }

        private async Task ApplyBalanceResults(BalanceTeamsResult balance)
        {
            if (!IsNullOrEmpty(balance.Message)) await SayBattle(balance.Message);
            if ((balance.Players != null) && (balance.Players.Count > 0))
            {
                foreach (var p in balance.Players)
                {
                    UserBattleStatus u;
                    if (Users.TryGetValue(p.Name, out u))
                    {
                        u.IsSpectator = p.IsSpectator;
                        u.AllyNumber = p.AllyID;
                    }
                }

                foreach (var u in Users.Where(x => !balance.Players.Any(y => y.Name == x.Key))) u.Value.IsSpectator = true;
            }

            if (balance.DeleteBots)
            {
                foreach (var b in Bots.Keys) await server.Broadcast(Users.Keys, new RemoveBot() { Name = b });
                Bots.Clear();
            }

            if ((balance.Bots != null) && (balance.Bots.Count > 0))
                foreach (var p in balance.Bots)
                    Bots.AddOrUpdate(p.BotName,
                        s => new BotBattleStatus(p.BotName, p.Owner ?? FounderName, p.BotAI) { AllyNumber = p.AllyID },
                        (s, status) =>
                        {
                            status.AllyNumber = p.AllyID;
                            status.owner = p.Owner ?? FounderName;
                            status.aiLib = p.BotAI;
                            status.Name = p.BotName;
                            return status;
                        });

            foreach (var u in Users.Values.Select(x => x.ToUpdateBattleStatus()).ToList()) await server.Broadcast(Users.Keys, u); // send other's status to self
            foreach (var u in Bots.Values.Select(x => x.ToUpdateBotStatus()).ToList()) await server.Broadcast(Users.Keys, u);
        }

        private async Task<bool> EnsureEngineIsPresent()
        {
            var down = server.Downloader.GetResource(DownloadType.ENGINE, EngineVersion);
            var task = down?.WaitHandle?.AsTask(TimeSpan.FromMinutes(3));
            if (task != null)
            {
                await SayBattle("Host downloading the engine");
                await task;
                if (down.IsComplete != true)
                {
                    await SayBattle("Host engine download failed");
                    return false;
                }
            }
            return true;
        }

        private void PickHostingPort()
        {
            var port = GlobalConst.UdpHostingPortStart;
            lock (pickPortLock)
            {
                var reservedPorts = server.Battles.Values.Where(x => x != null).Select(x => x.hostingPort).Distinct().ToDictionary(x => x, x => true);
                var usedPorts =
                    IPGlobalProperties.GetIPGlobalProperties()
                        .GetActiveUdpListeners()
                        .Where(x => x != null)
                        .Select(x => x.Port)
                        .Distinct()
                        .ToDictionary(x => x, x => true);

                while (usedPorts.ContainsKey(port) || reservedPorts.ContainsKey(port)) port++;
                hostingPort = port;
            }
        }


        private void pollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                pollTimer.Stop();
                if (ActivePoll != null) ActivePoll.End();
                StopVote();
            }
            catch { }
            finally
            {
                pollTimer.Start();
            }
        }


        private void SetupSpring()
        {
            spring?.UnsubscribeEvents(this);

            spring = new DedicatedServer(server.SpringPaths);

            spring.DedicatedServerExited += DedicatedServerExited;

            spring.DedicatedServerStarted += DedicatedServerStarted;
            spring.PlayerSaid += spring_PlayerSaid;
            spring.BattleStarted += spring_BattleStarted;
        }

        private void spring_BattleStarted(object sender, SpringBattleContext e)
        {
            try
            {
                StopVote();
                if (IsMatchMakerBattle && e.PlayersUnreadyOnStart.Count > 0 && e.IsTimeoutForceStarted)
                {
                    string message = string.Format("Players {0} did not choose a start position. Game will be aborted.", e.PlayersUnreadyOnStart.StringJoin());
                    spring.SayGame(message);
                    Trace.TraceInformation(string.Format("Matchmaker Game {0} aborted because {1}", BattleID, message));
                    RunCommandDirectly<CmdExit>(null);
                    server.UserLogSay($"Battle aborted because {e.PlayersUnreadyOnStart.Count} players didn't join their MM game: {e.PlayersUnreadyOnStart.StringJoin()}.");
                    e.PlayersUnreadyOnStart.ForEach(x => server.MatchMaker.BanPlayer(x));
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing spring_BattleStarted started: {0}", ex);
            }
        }


        private void spring_PlayerSaid(object sender, SpringChatEventArgs e)
        {
            try
            {
                ConnectedUser user;

                Say say = new Say() { User = e.Username, Text = e.Line, Place = SayPlace.Battle, AllowRelay = false };

                //dont broadcast commands
                if (CheckSayForCommand(say).Result) return;

                var isPlayer = spring.Context.ActualPlayers.Any(x => x.Name == e.Username && !x.IsSpectator);

                // block spectator chat in FFA and non chicken MM
                if (!isPlayer)
                {
                    if (spring.LobbyStartContext.Mode == AutohostMode.GameFFA ||
                        (spring.LobbyStartContext.IsMatchMakerGame && spring.LobbyStartContext.Mode != AutohostMode.GameChickens)) return;
                }

                // check bans
                if (!server.ConnectedUsers.TryGetValue(e.Username, out user) || user.User.BanMute || (user.User.BanSpecChat && !isPlayer))
                {
                    return;
                }

                // relay
                if (e.Location == SpringChatLocation.Public) server.GhostSay(say, BattleID);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing spring_PlayerSaid " + ex);
            }
        }

        private async void DedicatedServerExited(object sender, SpringBattleContext springBattleContext)
        {
            try
            {
                await OnDedicatedExited(springBattleContext);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing dedi server exited: {0}", ex);
            }
        }

        private void DedicatedServerStarted(object sender, EventArgs e)
        {
            try
            {
                StopVote();

                if (HostedMod?.Mission != null)
                {
                    var service = GlobalConst.GetContentService();
                    foreach (var u in spring.LobbyStartContext.Players.Where(x => !x.IsSpectator)) service.NotifyMissionRun(u.Name, HostedMod.Mission.Name);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing dedi server started: {0}", ex);
            }
        }

        public class KickedPlayer
        {
            public string Name;
            public DateTime TimeOfKicked = DateTime.UtcNow;
        }
    }
}
