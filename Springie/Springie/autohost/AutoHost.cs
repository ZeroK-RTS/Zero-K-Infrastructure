#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Timers;
using LobbyClient;
using PlasmaShared;
using PlasmaShared.ContentService;
using PlasmaShared.SpringieInterfaceReference;
using PlasmaShared.UnitSyncLib;
using Springie.autohost.Polls;
using ZkData;
using AutohostMode = PlasmaShared.SpringieInterfaceReference.AutohostMode;
using Timer = System.Timers.Timer;

#endregion

namespace Springie.autohost
{
    public partial class AutoHost
    {
        public const int PollTimeout = 60;
        readonly CommandList Commands;

        IVotable activePoll;
        string bossName = "";
        string delayedModChange;

        ResourceLinkProvider linkProvider;


        Timer pollTimer;
        string requestedEngineChange = null;
        readonly Timer timer;
        int timerTick = 0;

        public string BossName { get { return bossName; } set { bossName = value; } }
        public int CloneNumber { get; private set; }

        public SpawnConfig SpawnConfig { get; private set; }
        public MetaDataCache cache;
        public AhConfig config;

        public Mod hostedMod;
        public int hostingPort { get; private set; }
        public readonly Spring spring;

        public SpringPaths springPaths;
        public TasClient tas;

        int lastSplitPlayersCountCalled;

        public AutoHost(MetaDataCache cache, AhConfig config, int hostingPort, SpawnConfig spawn)
        {
            this.config = config;
            Commands = new CommandList(config);
            this.cache = cache;
            SpawnConfig = spawn;
            this.hostingPort = hostingPort;

            var version = !String.IsNullOrEmpty(config.SpringVersion) ? config.SpringVersion : Program.main.Config.SpringVersion;
            springPaths = new SpringPaths(Program.main.paths.GetEngineFolderByVersion(version), version, Program.main.Config.DataDir);

            Program.main.paths.SpringVersionChanged += (s, e) =>
                {
                    if (!String.IsNullOrEmpty(requestedEngineChange) && requestedEngineChange == Program.main.paths.SpringVersion)
                    {
                        config.SpringVersion = requestedEngineChange;
                        springPaths.SetEnginePath(Program.main.paths.GetEngineFolderByVersion(requestedEngineChange));
                        requestedEngineChange = null;

                        tas.Say(TasClient.SayPlace.Battle, "", "rehosting to engine version " + springPaths.SpringVersion, true);
                        ComRehost(TasSayEventArgs.Default, new string[] { });
                    }
                };

            spring = new Spring(springPaths) { UseDedicatedServer = true };
            bool isManaged= SpawnConfig == null && config.Mode != AutohostMode.None;

            tas = new TasClient(null, "Springie " + MainConfig.SpringieVersion, isManaged? GlobalConst.ZkSpringieManagedCpu:GlobalConst.ZkLobbyUserCpu, false, Program.main.Config.IpOverride);

            pollTimer = new Timer(PollTimeout*1000);
            pollTimer.Enabled = false;
            pollTimer.AutoReset = false;
            pollTimer.Elapsed += pollTimer_Elapsed;

            spring.SpringExited += spring_SpringExited;
            spring.GameOver += spring_GameOver;

            spring.SpringExited += spring_SpringExited;
            spring.SpringStarted += spring_SpringStarted;
            spring.PlayerSaid += spring_PlayerSaid;
            spring.BattleStarted += spring_BattleStarted;

            tas.BattleUserLeft += tas_BattleUserLeft;
            tas.UserStatusChanged += tas_UserStatusChanged;
            tas.BattleUserJoined += tas_BattleUserJoined;
            tas.MyBattleMapChanged += tas_MyBattleMapChanged;
            tas.BattleLockChanged += tas_BattleLockChanged;
            tas.BattleOpened += tas_BattleOpened;
            tas.UserAdded += (o, u) => { if (u.Data.Name == GetAccountName()) Start(null, null); };
            
            tas.RegistrationDenied += (s, e) =>
                {
                    ErrorHandling.HandleException(null, "Registration denied: " + e.ServerParams[0]);
                    CloneNumber++;
                    tas.Login(GetAccountName(), config.Password);
                };

            tas.RegistrationAccepted += (s, e) => tas.Login(GetAccountName(), config.Password);

            tas.AgreementRecieved += (s, e) =>
                {
                    tas.AcceptAgreement();

                    PlasmaShared.Utils.SafeThread(() =>
                        {
                            Thread.Sleep(7000);
                            tas.Login(GetAccountName(), config.Password);
                        }).Start();
                };

            tas.ConnectionLost += tas_ConnectionLost;
            tas.Connected += tas_Connected;
            tas.LoginDenied += tas_LoginDenied;
            tas.LoginAccepted += tas_LoginAccepted;
            tas.Said += tas_Said;
            tas.MyBattleStarted += tas_MyStatusChangedToInGame;
            
            linkProvider = new ResourceLinkProvider(this);

            tas.Connect(Program.main.Config.ServerHost, Program.main.Config.ServerPort);

            Program.main.Downloader.PackagesChanged += Downloader_PackagesChanged;

            timer = new Timer(15000);
            timer.Elapsed += (s, e) =>
                {
                    try
                    {
                        timer.Stop();
                        timerTick++;

                        // auto update engine branch
                        if (!String.IsNullOrEmpty(config.AutoUpdateSpringBranch) && timerTick % 4 == 0) CheckEngineBranch();

                        // auto verify pw map
                        if (!spring.IsRunning && config.Mode != AutohostMode.None)
                        {
                            if (SpawnConfig == null && config.Mode == AutohostMode.Planetwars) ServerVerifyMap(false); 
                        }


                        // auto start split vote
                        if (config.SplitBiggerThan != null && tas.MyBattle != null && config.SplitBiggerThan < tas.MyBattle.NonSpectatorCount) {
                            var cnt = tas.MyBattle.NonSpectatorCount;
                            if (cnt > lastSplitPlayersCountCalled && cnt%2 == 0) {
                                StartVote(new VoteSplitPlayers(tas, spring, this), TasSayEventArgs.Default, new string[] { });
                                lastSplitPlayersCountCalled = cnt;
                            }
                        }

                        // auto rehost to latest mod version
                        if (!spring.IsRunning && delayedModChange != null && cache.GetResourceDataByInternalName(delayedModChange) != null)
                        {
                            var mod = delayedModChange;
                            delayedModChange = null;
                            config.Mod = mod;
                            SayBattle("Updating to latest mod version: " + mod);
                            ComRehost(TasSayEventArgs.Default, new[] { mod });
                        }



                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                    finally {
                        timer.Start();
                    }
                };
            timer.Start();
        }

        void spring_BattleStarted(object sender, EventArgs e)
        {
            StopVote();
        }

        public void Dispose()
        {
            Stop();
            tas.UnsubscribeEvents(this);
            spring.UnsubscribeEvents(this);
            springPaths.UnsubscribeEvents(this);
            Program.main.Downloader.UnsubscribeEvents(this);
            Program.main.paths.UnsubscribeEvents(this);
            tas.Disconnect();
            pollTimer.Dispose();
            if (timer != null) timer.Dispose();
            pollTimer = null;
            linkProvider = null;
        }

        public string GetAccountName()
        {
            if (CloneNumber > 0) return config.Login + CloneNumber;
            else return config.Login;
        }

        /*void fileDownloader_DownloadProgressChanged(object sender, TasEventArgs e)
    {
      if (tas.IsConnected) {
        SayBattle(e.ServerParams[0] + " " + e.ServerParams[1] + "% done");
      }
    }*/


        public int GetUserLevel(TasSayEventArgs e)
        {
            return GetUserLevel(e.UserName);
        }

        public int GetUserLevel(string name)
        {
            var ret = tas.ExistingUsers[name].SpringieLevel;
            if (!String.IsNullOrEmpty(bossName))
            {
                if (name == bossName) ret += 1;
                else ret += -1;
            }
            return ret;
        }


        public bool HasRights(string command, TasSayEventArgs e)
        {
            foreach (var c in Commands.Commands)
            {
                if (c.Name == command)
                {
                    if (c.Throttling > 0)
                    {
                        var diff = (int)DateTime.Now.Subtract(c.lastCall).TotalSeconds;
                        if (diff < c.Throttling)
                        {
                            Respond(e, "AntiSpam - please wait " + (c.Throttling - diff) + " more seconds");
                            return false;
                        }
                    }

                    for (var i = 0; i < c.ListenTo.Length; i++)
                    {
                        if (c.ListenTo[i] == e.Place)
                        {
                            // command is only for nonspecs
                            if (!c.AllowSpecs) {
                                if (tas.MyBattle == null || !tas.MyBattle.Users.Any(x => x.LobbyUser.Name == e.UserName && !x.IsSpectator)) return false;
                            }

                            var reqLevel = c.Level;
                            var ulevel = GetUserLevel(e);

                            if (ulevel >= reqLevel)
                            {
                                c.lastCall = DateTime.Now;
                                return true; // ALL OK
                            }
                            else
                            {
                                if (e.Place == TasSayEventArgs.Places.Battle && tas.MyBattle != null && tas.MyBattle.NonSpectatorCount == 1 &&
                                    (!command.StartsWith("vote") && HasRights("vote" + command, e)))
                                {
                                    // server only has 1 player and we have rights for vote variant - we might as well just do it
                                    return true;
                                }
                                else
                                {
                                    Respond(e,
                                            String.Format("Sorry, you do not have rights to execute {0}{1}",
                                                          command,
                                                          (!string.IsNullOrEmpty(bossName) ? ", ask boss admin " + bossName : "")));
                                    return false;
                                }
                            }
                        }
                    }
                    return false; // place not allowed for this command = ignore command
                }
            }
            if (e.Place != TasSayEventArgs.Places.Channel) Respond(e, "Sorry, I don't know command '" + command + "'");
            return false;
        }

        public void RegisterVote(TasSayEventArgs e, bool vote)
        {
            if (activePoll != null)
            {
                if (activePoll.Vote(e, vote)) {
                    pollTimer.Enabled = false;
                    activePoll = null;
                }
            }
            else Respond(e, "There is no poll going on, start some first");
        }

        public void Respond(TasSayEventArgs e, string text)
        {
            Respond(tas, spring, e, text);
        }

        public static void Respond(TasClient tas, Spring spring, TasSayEventArgs e, string text)
        {
            var p = TasClient.SayPlace.User;
            var emote = false;
            if (e.Place == TasSayEventArgs.Places.Battle)
            {
                p = TasClient.SayPlace.BattlePrivate;
                emote = true;
            }
            if (e.Place == TasSayEventArgs.Places.Game && spring.IsRunning) spring.SayGame(text);
            else tas.Say(p, e.UserName, text, emote);
        }

        public void RunCommand(string text)
        {
            var allwords = text.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (allwords.Length < 1) return;
            var com = allwords[0];
            // remove first word (command)
            var words = Utils.ShiftArray(allwords, -1);
            RunCommand(TasSayEventArgs.Default, com, words);
        }

        public void RunCommand(TasSayEventArgs e, string com, string[] words)
        {
            switch (com)
            {
                case "listmaps":
                    ComListMaps(e, words);
                    break;

                case "listmods":
                    ComListMods(e, words);
                    break;

                case "help":
                    ComHelp(e, words);
                    break;

                case "map":
                    ComMap(e, words);
                    break;

                case "admins":
                    ComAdmins(e, words);
                    break;

                case "start":
                    if (tas.MyBattle != null) {
                        var cnt = tas.MyBattle.NonSpectatorCount;
                        if (cnt == 1) ComStart(e,words);
                        else {
                            StartVote(new VoteStart(tas, spring, this), e, words); 
                        }
                    }
                    
                    break;

                case "forcestart":
                    ComForceStart(e, words);
                    break;

                case "force":
                    ComForce(e, words);
                    break;

                case "split":
                    ComSplit(e, words);
                    break;

                case "corners":
                    ComCorners(e, words);
                    break;

                case "maplink":
                    linkProvider.FindLinks(words, ResourceLinkProvider.FileType.Map, tas, e);
                    break;

                case "modlink":
                    linkProvider.FindLinks(words, ResourceLinkProvider.FileType.Mod, tas, e);
                    break;

                case "ring":
                    ComRing(e, words);
                    break;

                case "kick":
                        ComKick(e, words);
                    break;

                case "exit":
                    ComExit(e, words);
                    break;

                case "lock":
                    tas.ChangeLock(true);
                    break;

                case "unlock":
                    tas.ChangeLock(false);
                    break;

                case "vote":
                    RegisterVote(e, words.Length < 1 || words[0] != "2");
                    break;

                case "y":
                    RegisterVote(e, true);
                    break;

                case "n":
                    RegisterVote(e, false);
                    break;

                case "votemap":
                    StartVote(new VoteMap(tas, spring, this), e, words);
                    break;

                case "votekick":
                    StartVote(new VoteKick(tas, spring, this), e, words);
                    break;

                case "votespec":
                    StartVote(new VoteSpec(tas, spring, this), e, words);
                    break;

                case "voteresign":
                    StartVote(new VoteResign(tas, spring, this), e, words);
                    break;

                case "voteforcestart":
                    StartVote(new VoteForceStart(tas, spring, this), e, words);
                    break;

                case "voteforce":
                    StartVote(new VoteForce(tas, spring, this), e, words);
                    break;

                case "voteexit":
                    StartVote(new VoteExit(tas, spring, this), e, words);
                    break;

                case "predict":
                    ComPredict(e, words);
                    break;

                case "fix":
                    ComFix(e, words);
                    break;

                case "rehost":
                    ComRehost(e, words);
                    break;

                case "voterehost":
                    StartVote(new VoteRehost(tas, spring, this), e, words);
                    break;

                case "random":
                    ComRandom(e, words);
                    break;

                case "balance":
                    ComBalance(e, words);
                    break;

                case "say":
                    ComSay(e, words);
                    break;

                case "id":
                    ComTeam(e, words);
                    break;

                case "team":
                    ComAlly(e, words);
                    break;

                case "resetoptions":
                    ComResetOptions(e,words);
                    break;

                case "helpall":
                    ComHelpAll(e, words);
                    break;

                case "fixcolors":
                    ComFixColors(e, words);
                    break;

                case "teamcolors":
                    ComTeamColors(e, words);
                    break;

                case "springie":
                    ComSpringie(e, words);
                    break;

                case "endvote":
                    StopVote();
                    SayBattle("poll cancelled");
                    break;

                case "addbox":
                    ComAddBox(e, words);
                    break;

                case "clearbox":
                    ComClearBox(e, words);
                    break;

                case "cbalance":
                    ComCBalance(e, words);
                    break;



                case "notify":
                    ComNotify(e, words);
                    break;

                case "boss":
                    ComBoss(e, words);
                    break;

                case "voteboss":
                    StartVote(new VoteBoss(tas, spring, this), e, words);
                    break;

                case "setpassword":
                    ComSetPassword(e, words);
                    break;

                case "setgametitle":
                    ComSetGameTitle(e, words);
                    break;

                case "setmaxplayers":
                    ComSetMaxPlayers(e, words);
                    break;

                case "spec":
                    ComForceSpectator(e, words);
                    break;

                case "specafk":
                    ComForceSpectatorAfk(e, words);
                    break;


                case "saveboxes":
                    ComSaveBoxes(e, words);
                    break;

                case "cheats":
                    if (spring.IsRunning)
                    {
                        spring.SayGame("/cheat");
                        SayBattle("Cheats!");
                    }
                    else Respond(e, "Cannot set cheats, game not running");
                    break;

                case "listoptions":
                    ComListOptions(e, words);
                    break;

                case "setoptions":
                    ComSetOption(e, words);
                    break;

                case "votesetoptions":
                    StartVote(new VoteSetOptions(tas, spring, this), e, words);
                    break;

                case "splitplayers":
                    ComSplitPlayers(e, words);
                    break;

                case "votesplitplayers":
                    StartVote(new VoteSplitPlayers(tas, spring, this), e, words);
                    break;

                case "setengine":
                    ComSetEngine(e, words);
                    break;

                case "transmit":
                    ComTransmit(e, words);
                    break;

                case "move":
                    ComMove(e,words);
                    break;

                case "votemove":
                    StartVote(new VoteMove(tas,spring,this),e,words );
                    break;

                case "juggle":
                    ComJuggle(e,words);
                    break;

                case "spawn":
                {
                    var args = Utils.Glue(words);
                    if (String.IsNullOrEmpty(args))
                    {
                        Respond(e, "Please specify parameters");
                        return;
                    }
                    var configKeys = new Dictionary<string, string>();
                    foreach (var f in args.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = f.Split('=');
                        if (parts.Length == 2) configKeys[parts[0].Trim()] = parts[1].Trim();
                    }
                    var sc = new SpawnConfig(e.UserName, configKeys);
                    if (String.IsNullOrEmpty(sc.Mod))
                    {
                        Respond(e, "Please specify at least mod name: !spawn mod=zk:stable");
                        return;
                    }
                    Program.main.SpawnAutoHost(config, sc);
                }
                    break;
            }
        }


        public void SayBattle(string text)
        {
            SayBattle(text, true);
        }

        public void SayBattlePrivate(string user, string text)
        {
            if (!String.IsNullOrEmpty(text)) foreach (var line in text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) tas.Say(TasClient.SayPlace.BattlePrivate, user, text, true);
        }

        public void SayBattle(string text, bool ingame)
        {
            if (!String.IsNullOrEmpty(text)) foreach (var line in text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) SayBattle(tas, spring, line, ingame);
        }


        public static void SayBattle(TasClient tas, Spring spring, string text, bool ingame)
        {
            tas.Say(TasClient.SayPlace.Battle, "", text, true);
            if (spring.IsRunning && ingame) spring.SayGame(text);
        }

        public void Start(string modname, string mapname)
        {
            Stop();

            if (String.IsNullOrEmpty(modname)) modname = config.Mod;
            if (String.IsNullOrEmpty(mapname)) mapname = config.Map;

            if (!String.IsNullOrEmpty(config.AutoUpdateRapidTag)) modname = config.AutoUpdateRapidTag;

            var title = config.Title.Replace("%1", MainConfig.SpringieVersion);
            var password = "*";
            if (!string.IsNullOrEmpty(config.BattlePassword)) password = config.BattlePassword;

            if (SpawnConfig != null)
            {
                modname = SpawnConfig.Mod;
                title = SpawnConfig.Title;
                if (!String.IsNullOrEmpty(SpawnConfig.Password)) password = SpawnConfig.Password;
            }

            //title = title + string.Format(" [engine{0}]", springPaths.SpringVersion);

            var version = Program.main.Downloader.PackageDownloader.GetByTag(modname);
            if (version != null && cache.GetResourceDataByInternalName(version.InternalName) != null) modname = version.InternalName;

            hostedMod = new Mod();
            cache.GetMod(modname, (m) => { hostedMod = m; }, (m) => { }, springPaths.SpringVersion);
            if (hostedMod.IsMission && !String.IsNullOrEmpty(hostedMod.MissionMap)) mapname = hostedMod.MissionMap;

            Map mapi = null;
            cache.GetMap(mapname, (m, x, y, z) => { mapi = m; }, (e) => { }, springPaths.SpringVersion);
            //int mint, maxt;
            var b = new Battle(springPaths.SpringVersion, password, hostingPort, config.MaxPlayers, 0, mapi, title, hostedMod, new BattleDetails());
            // if hole punching enabled then we use it
            if (Program.main.Config.UseHolePunching) b.Nat = Battle.NatMode.HolePunching;
            else if (Program.main.Config.GargamelMode) b.Nat = Battle.NatMode.FixedPorts;
            else b.Nat = Battle.NatMode.None; // else either no nat or fixed ports (for gargamel fake - to get client IPs)
            tas.OpenBattle(b);
            
        }


        public void StartVote(IVotable vote, TasSayEventArgs e, string[] words)
        {
            if (vote != null)
            {
                if (activePoll != null)
                {
                    Respond(e, "Another poll already in progress, please wait");
                    return;
                }
                if (vote.Setup(e, words))
                {
                    activePoll = vote;
                    pollTimer.Interval = PollTimeout*1000;
                    pollTimer.Enabled = true;
                }
            }
        }


        public void Stop()
        {
            StopVote();
            spring.ExitGame();
            tas.ChangeMyUserStatus(false, false);
            tas.LeaveBattle();
        }

        public void StopVote()
        {
            if (activePoll != null) activePoll.End();
            pollTimer.Enabled = false;
            activePoll = null;
        }

        void CheckEngineBranch()
        {
            var url = String.Format("http://springrts.com/dl/buildbot/default/{0}/LATEST", config.AutoUpdateSpringBranch);
            try
            {
                var wc = new WebClient();
                var str = wc.DownloadString(url);
                var bstr = "{" + config.AutoUpdateSpringBranch + "}";
                if (str.StartsWith(bstr)) str = str.Replace(bstr, "");
                str = str.Trim('\n', '\r', ' ');

                if (springPaths.SpringVersion != str) ComSetEngine(TasSayEventArgs.Default, new string[] { str });
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error getting latest engine branch version from {0}: {1}");
            }
        }


        public bool JuggleIfNeeded() {
            if (tas.MyBattle != null && !spring.IsRunning && config != null && SpawnConfig == null)
            {
                var count = tas.MyBattle.Users.Count(x => !x.IsSpectator && x.SyncStatus != SyncStatuses.Unknown);
                if (count > (config.SplitBiggerThan??99) || (count> 0 && count<(config.MinToJuggle??0)))
                {
                    Program.main.JugglePlayers();
                    return true;
                }
            }
            return false;
        }


        void CheckForBattleExit()
        {
            if ((DateTime.Now - spring.GameStarted) > TimeSpan.FromSeconds(20))
            {
                if (spring.IsRunning)
                {
                    var b = tas.MyBattle;
                    var count = 0;
                    foreach (var p in b.Users)
                    {
                        if (p.IsSpectator) continue;

                        User u;
                        if (!tas.GetExistingUser(p.Name, out u)) continue;
                        if (u.IsInGame) count++;
                    }
                    if (count < 1)
                    {
                        SayBattle("closing game, " + count + " active player left in game");
                        spring.ExitGame();
                    }
                }
                // kontrola pro pripad ze by se nevypl spring
                User us;
                if (!spring.IsRunning && tas.GetExistingUser(tas.UserName, out us) && us.IsInGame) tas.ChangeMyUserStatus(false, false);
            }
        }

        /// <summary>
        /// Gets free slots, first mandatory then optional
        /// </summary>
        /// <returns></returns>
        IEnumerable<MissionSlot> GetFreeSlots()
        {
            var b = tas.MyBattle;
            return
                hostedMod.MissionSlots.Where(x => x.IsHuman).OrderByDescending(x => x.IsRequired).Where(
                    x => !b.Users.Any(y => y.AllyNumber == x.AllyID && y.TeamNumber == x.TeamID && !y.IsSpectator));
        }

        void Downloader_PackagesChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(config.AutoUpdateRapidTag) && SpawnConfig == null)
            {
                var version = Program.main.Downloader.PackageDownloader.GetByTag(config.AutoUpdateRapidTag);
                if (version != null)
                {
                    var latest = version.InternalName;
                    if (!String.IsNullOrEmpty(latest) && (tas.MyBattle == null || tas.MyBattle.ModName != latest)) {
                        if (cache.GetResourceDataByInternalName(latest) != null && !spring.IsRunning)
                        {
                            config.Mod = latest;
                            SayBattle("Updating to latest mod version: " + latest);
                            ComRehost(TasSayEventArgs.Default, new[] { latest });
                        } else delayedModChange = latest;
                    }
                }
            }
        }


        void pollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                pollTimer.Stop();
                if (activePoll != null) activePoll.End();
                StopVote();
            }
            catch { }
            finally {pollTimer.Start();}
        }

        void spring_GameOver(object sender, SpringLogEventArgs e)
        {
            SayBattle("Game over, exiting");
            PlasmaShared.Utils.SafeThread(() =>
                {
                    Thread.Sleep(5000); // wait for stats
                    spring.ExitGame();
                }).Start();

            /*			try
			{
				var service = new ContentService();
				service.GetRecommendedMap()

			}

			if (config.MapCycle.Length > 0)
			{
				mapCycleIndex = mapCycleIndex%config.MapCycle.Length;
				SayBattle("changing to another map in mapcycle");
				ComMap(TasSayEventArgs.Default, config.MapCycle[mapCycleIndex].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
				mapCycleIndex++;
			}*/
        }


        void spring_PlayerSaid(object sender, SpringLogEventArgs e)
        {
            tas.GameSaid(e.Username, e.Line);
            User us;
            tas.ExistingUsers.TryGetValue(e.Username, out us);
            var isMuted = us != null && us.BanMute;
            if (Program.main.Config.RedirectGameChat && e.Username != tas.UserName && !e.Line.StartsWith("Allies:") &&
                !e.Line.StartsWith("Spectators:") && !isMuted) tas.Say(TasClient.SayPlace.Battle, "", "[" + e.Username + "]" + e.Line, false);
        }



        void spring_SpringExited(object sender, EventArgs e)
        {
            StopVote();
            tas.ChangeLock(false);
            tas.ChangeMyUserStatus(false, false);
            var b = tas.MyBattle;
            foreach (var s in toNotify)
            {
                if (b != null && b.Users.Any(x => x.Name == s)) tas.Ring(s);
                tas.Say(TasClient.SayPlace.User, s, "** Game just ended, join me! **", false);
            }
            toNotify.Clear();

            if (SpawnConfig == null && DateTime.Now.Subtract(spring.GameStarted).TotalMinutes >5) ServerVerifyMap(true);
            if (SpawnConfig == null) Program.main.RequestJuggle();

        }

        void spring_SpringStarted(object sender, EventArgs e)
        {
            tas.ChangeLock(false);
            if (hostedMod.IsMission) using (var service = new ContentService() { Proxy = null }) foreach (var u in tas.MyBattle.Users.Where(x => !x.IsSpectator)) service.NotifyMissionRunAsync(u.Name, hostedMod.ShortName);
            StopVote();
        }


        void tas_BattleLockChanged(object sender, BattleInfoEventArgs e1)
        {
            if (e1.BattleID == tas.MyBattleID) SayBattle("game " + (tas.MyBattle.IsLocked ? "locked" : "unlocked"), false);
        }

        void tas_BattleOpened(object sender, TasEventArgs e)
        {
            tas.ChangeMyBattleStatus(true, false, SyncStatuses.Synced);
            if (hostedMod.IsMission)
            {
                foreach (var slot in hostedMod.MissionSlots.Where(x => !x.IsHuman))
                {
                    var ubs = new UserBattleStatus();
                    ubs.SyncStatus = SyncStatuses.Synced;
                    ubs.TeamColor = slot.Color;
                    ubs.AllyNumber = slot.AllyID;
                    ubs.TeamNumber = slot.TeamID;
                    ubs.IsReady = true;
                    ubs.IsSpectator = false;
                    ubs.Name = slot.AiShortName;
                    tas.AddBot(slot.TeamName, ubs, slot.Color, slot.AiShortName);
                }
            }
            if (SpawnConfig != null) tas.Say(TasClient.SayPlace.User, SpawnConfig.Owner, "I'm here! Ready to serve you! Join me!", false);
            else ServerVerifyMap(true);
        }


        void tas_BattleUserJoined(object sender, BattleUserEventArgs e1)
        {
            if (e1.BattleID != tas.MyBattleID) return;
            var name = e1.UserName;

            if (tas.ExistingUsers[name].BanLobby) {
                tas.Kick(name);
                return;
            }

            var welc = config.Welcome;
            if (welc != "")
            {
                welc = welc.Replace("%1", name);
                welc = welc.Replace("%2", GetUserLevel(name).ToString());
                welc = welc.Replace("%3", MainConfig.SpringieVersion);
                SayBattlePrivate(name, welc);
            }
            if (spring.IsRunning)
            {
                spring.AddUser(e1.UserName, e1.ScriptPassword);
                var started = DateTime.Now.Subtract(spring.GameStarted);
                started = new TimeSpan((int)started.TotalHours, started.Minutes, started.Seconds);
                SayBattlePrivate(name, String.Format("GAME IS CURRENTLY IN PROGRESS, PLEASE WAIT TILL IT ENDS! Running for {0}", started));
                SayBattlePrivate(name, "If you say !notify, I will PM you when game ends.");
            }

            if (SpawnConfig == null)
            {
                try
                {
                    var serv = new SpringieService();
                    var ret = serv.AutohostPlayerJoined(tas.MyBattle.GetContext(), tas.ExistingUsers[name].LobbyID);
                    if (ret != null)
                    {
                        if (!string.IsNullOrEmpty(ret.PrivateMessage)) tas.Say(TasClient.SayPlace.User, name, ret.PrivateMessage, false);
                        if (!string.IsNullOrEmpty(ret.PublicMessage)) tas.Say(TasClient.SayPlace.Battle, "", ret.PublicMessage, true);
                        if (ret.ForceSpec) tas.ForceSpectator(name);
                        if (ret.Kick) tas.Kick(name);
                    }
                }
                catch (Exception ex)
                {
                    SayBattle("ServerManage error: " + ex, false);
                }
            }

            if (SpawnConfig != null && SpawnConfig.Owner == name) // owner joins, set him boss 
                ComBoss(TasSayEventArgs.Default, new[] { name });

            JuggleIfNeeded();
        }

        void tas_BattleUserLeft(object sender, BattleUserEventArgs e1)
        {
            if (e1.BattleID != tas.MyBattleID) return;
            CheckForBattleExit();

            if (spring.IsRunning) spring.SayGame(e1.UserName + " has left lobby");

            if (e1.UserName == bossName)
            {
                SayBattle("boss has left the battle");
                bossName = "";
            }

            var battle = tas.MyBattle;
            if (battle.IsLocked && battle.Users.Count < 2)
            {
                // player left and only 2 remaining (springie itself + some noob) -> unlock
                tas.ChangeLock(false);
            }
        }


        // login accepted - join channels

        // im connected, let's login
        void tas_Connected(object sender, TasEventArgs e)
        {
            tas.Login(GetAccountName(), config.Password);
        }


        void tas_ConnectionLost(object sender, TasEventArgs e)
        {
            Stop();
        }


        void tas_LoginAccepted(object sender, TasEventArgs e)
        {
            foreach (var c in config.JoinChannels) tas.JoinChannel(c);
        }

        void tas_LoginDenied(object sender, TasEventArgs e)
        {
            if (e.ServerParams[0] == "Bad username/password") tas.Register(GetAccountName(), config.Password);
            else
            {
                CloneNumber++;
                tas.Login(GetAccountName(), config.Password);
            }
        }

        void tas_MyBattleMapChanged(object sender, BattleInfoEventArgs e1)
        {
            var b = tas.MyBattle;
            var mapName = b.MapName.ToLower();

            if (SpawnConfig == null) ComResetOptions(TasSayEventArgs.Default, new string[]{});

            try
            {
                var serv = new SpringieService();
                var commands = serv.GetMapCommands(mapName);
                if (!string.IsNullOrEmpty(commands)) foreach (var c in commands.Split('\n').Where(x => !string.IsNullOrEmpty(x))) RunCommand(c);
            }
            catch (Exception ex) {
                Trace.TraceError("Error procesing map commands: {0}",ex);
            }

        }

        void tas_MyStatusChangedToInGame(object sender, TasEventArgs e)
        {
            spring.StartGame(tas, Program.main.Config.HostingProcessPriority, null, null);
        }

        void tas_Said(object sender, TasSayEventArgs e)
        {
            if (String.IsNullOrEmpty(e.UserName)) return;
            if (Program.main.Config.RedirectGameChat && e.Place == TasSayEventArgs.Places.Battle && e.Origin == TasSayEventArgs.Origins.Player &&
                e.UserName != tas.UserName && e.IsEmote == false && !tas.ExistingUsers[e.UserName].BanMute) spring.SayGame("[" + e.UserName + "]" + e.Text);

            // check if it's command
            if (e.Origin == TasSayEventArgs.Origins.Player && !e.IsEmote && e.Text.StartsWith("!"))
            {
                if (e.Text.Length < 2) return;
                var allwords = e.Text.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (allwords.Length < 1) return;
                var com = allwords[0];

                // remove first word (command)
                var words = Utils.ShiftArray(allwords, -1);

                if (!HasRights(com, e))
                {
                    if (!com.StartsWith("vote"))
                    {
                        com = "vote" + com;

                        if (!Commands.Commands.Any(x => x.Name == com) || !HasRights(com, e)) return;
                    }
                    else return;
                }

                if (e.Place == TasSayEventArgs.Places.Normal)
                {
                    if (com != "say" && com != "admins" && com != "help" && com != "helpall" && com != "springie" && com != "listoptions" &&
                        com != "spawn" && com != "predict" && com != "notify" && com!="transmit") SayBattle(String.Format("{0} executed by {1}", com, e.UserName));
                }

                RunCommand(e, com, words);
            }
        }


        void tas_UserStatusChanged(object sender, TasEventArgs e)
        {
            if (spring.IsRunning)
            {
                var b = tas.MyBattle;
                if (e.ServerParams[0] != tas.UserName && b.Users.Any(x => x.Name == e.ServerParams[0])) CheckForBattleExit();
            }
            JuggleIfNeeded();
        }
    }
}