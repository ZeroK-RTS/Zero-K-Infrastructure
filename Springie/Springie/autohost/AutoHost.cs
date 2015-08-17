#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Timers;
using LobbyClient;
using PlasmaDownloader.Packages;
using PlasmaShared;
using ZkData.UnitSyncLib;
using Springie.autohost.Polls;
using ZkData;
using Timer = System.Timers.Timer;

#endregion

namespace Springie.autohost
{
    public partial class AutoHost
    {
        public const int PollTimeout = 60;
        const int GameExitSplitDelay = 120;
        public readonly CommandList Commands;

        IVotable activePoll;
        string bossName = "";
        string delayedModChange;
        int lastSplitPlayersCountCalled;

        ResourceLinkSpringieClient linkSpringieClient;


        Timer pollTimer;
        string requestedEngineChange;
        readonly Timer timer;
        int timerTick;

        public string BossName { get { return bossName; } set { bossName = value; } }
        public int CloneNumber { get; set; }

        public SpawnConfig SpawnConfig { get; private set; }
        public MetaDataCache cache;
        public AhConfig config;
        public MatchMakerQueue queue;

        public Mod hostedMod;
        public int hostingPort { get; private set; }
        public readonly Spring spring;

        public SpringPaths springPaths;
        public TasClient tas;

        public AutoHost(MetaDataCache cache, AhConfig config, int hostingPort, SpawnConfig spawn) {
            this.config = config;
            Commands = new CommandList(config);
            this.cache = cache;
            SpawnConfig = spawn;
            this.hostingPort = hostingPort;

            
            string version = config.SpringVersion ?? Program.main.Config.SpringVersion ?? GlobalConst.DefaultEngineOverride;
            springPaths = new SpringPaths(Program.main.paths.GetEngineFolderByVersion(version), Program.main.Config.DataDir);
            
            springPaths.SpringVersionChanged += (s, e) =>
                {
                    if (!String.IsNullOrEmpty(requestedEngineChange) && requestedEngineChange == springPaths.SpringVersion) {
                        config.SpringVersion = requestedEngineChange;
                        springPaths.SetEnginePath(Program.main.paths.GetEngineFolderByVersion(requestedEngineChange));
                        requestedEngineChange = null;

                        tas.Say(SayPlace.Battle, "", "rehosting to engine version " + springPaths.SpringVersion, true);
                        ComRehost(TasSayEventArgs.Default, new string[] { });
                    }
                };

            spring = new Spring(springPaths) { UseDedicatedServer = true };
            bool isManaged = SpawnConfig == null && config.Mode != AutohostMode.None;

            tas = new TasClient(MainConfig.SpringieVersion,
                                isManaged ? Login.ClientTypes.SpringieManaged : Login.ClientTypes.Springie,
                                Program.main.Config.IpOverride);

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
            tas.BattleOpened += tas_BattleOpened;
            tas.UserAdded += (o, u) => { if (u.Name == GetAccountName()) OpenBattleRoom(null, null); };

            tas.RegistrationDenied += (s, e) =>
                {
                    Trace.TraceWarning("Registration denied: {0} {1}", e.ResultCode.Description(), e.Reason);
                    CloneNumber++;
                    tas.Login(GetAccountName(), config.Password);
                };

            tas.RegistrationAccepted += (s, e) => tas.Login(GetAccountName(), config.Password);

            tas.ConnectionLost += tas_ConnectionLost;
            tas.Connected += tas_Connected;
            tas.LoginDenied += tas_LoginDenied;
            tas.LoginAccepted += tas_LoginAccepted;
            tas.Said += tas_Said;
            tas.MyBattleStarted += tas_MyStatusChangedToInGame;

            linkSpringieClient = new ResourceLinkSpringieClient(this);

            // queue autohost
            if (config != null && config.MinToJuggle != null && SpawnConfig == null)
            {
                queue = new MatchMakerQueue(this);
            }

            
            Program.main.Downloader.PackagesChanged += Downloader_PackagesChanged;

            timer = new Timer(15000);
            timer.Elapsed += (s, e) =>
                {
                    try {
                        timer.Stop();
                        timerTick++;

                        // auto update engine branch
                        if (!String.IsNullOrEmpty(config.AutoUpdateSpringBranch) && timerTick%4 == 0) CheckEngineBranch();

                        // auto verify pw map
                        if (!spring.IsRunning && config.Mode != AutohostMode.None) if (SpawnConfig == null && config.Mode == AutohostMode.Planetwars) ServerVerifyMap(false);

                        // auto start split vote
                        if (!spring.IsRunning && config.SplitBiggerThan != null && tas.MyBattle != null && config.SplitBiggerThan < tas.MyBattle.NonSpectatorCount) {
                            if (DateTime.Now.Subtract(spring.GameExited).TotalSeconds >= GameExitSplitDelay) ComSplitPlayers(TasSayEventArgs.Default, new string[]{});
                            /*
                            int cnt = tas.MyBattle.NonSpectatorCount;
                            if (cnt > lastSplitPlayersCountCalled && cnt%2 == 0) {
                                StartVote(new VoteSplitPlayers(tas, spring, this), TasSayEventArgs.Default, new string[] { });
                                lastSplitPlayersCountCalled = cnt;
                            }*/
                        }

                        // auto rehost to latest mod version
                        if (!string.IsNullOrEmpty(config.AutoUpdateRapidTag) && SpawnConfig == null) UpdateRapidMod(config.AutoUpdateRapidTag);


                    } catch (Exception ex) {
                        Trace.TraceError(ex.ToString());
                    } finally {
                        timer.Start();
                    }
                };
            timer.Start();
           
        }

        public void Start()
        {
            tas.Connect(Program.main.Config.ServerHost, Program.main.Config.ServerPort);
        }

        public void Dispose() {
            Stop();
            tas.UnsubscribeEvents(this);
            spring.UnsubscribeEvents(this);
            springPaths.UnsubscribeEvents(this);
            Program.main.Downloader.UnsubscribeEvents(this);
            Program.main.paths.UnsubscribeEvents(this);
            tas.RequestDisconnect();
            pollTimer.Dispose();
            if (timer != null) timer.Dispose();
            pollTimer = null;
            linkSpringieClient = null;
        }

        public string GetAccountName() {
            if (CloneNumber > 0) return config.Login + CloneNumber;
            else return config.Login;
        }

        /*void fileDownloader_DownloadProgressChanged(object sender, TasEventArgs e)
    {
      if (tas.IsConnected) {
        SayBattle(e.ServerParams[0] + " " + e.ServerParams[1] + "% done");
      }
    }*/

	public bool GetUserAdminStatus(TasSayEventArgs e) {
		if (!tas.ExistingUsers.ContainsKey(e.UserName)) return false;
		if (!String.IsNullOrEmpty(bossName) && (bossName == e.UserName)) return true;
		return tas.ExistingUsers[e.UserName].IsAdmin;
	}

	public bool GetUserIsSpectator (TasSayEventArgs e) {
		if (tas.MyBattle == null) return true;
		if (spring.IsRunning)  {
			PlayerTeam user = spring.StartContext.Players.FirstOrDefault(x => x.Name == e.UserName && !x.IsSpectator);
			return ((user == null) || user.IsSpectator);
		} else {
			return !tas.MyBattle.Users.Values.Any(x => x.LobbyUser.Name == e.UserName && !x.IsSpectator);
		}
	}

        public int GetUserLevel(TasSayEventArgs e) {
            if (!tas.ExistingUsers.ContainsKey(e.UserName))
            {
                //Respond(e, string.Format("{0} please reconnect to lobby for right verification", e.UserName));
                return 0; //1 is default, but we return 0 to avoid right abuse (by Disconnecting from Springie and say thru Spring)
            }
            return GetUserLevel(e.UserName);
        }

        public int GetUserLevel(string name) {
            int ret = tas.ExistingUsers[name].SpringieLevel;
            if (!String.IsNullOrEmpty(bossName)) {
                if (name == bossName) ret = Math.Max(GlobalConst.SpringieBossEffectiveRights, ret);
                else ret += -1;
            }
            return ret;
        }


        public bool HasRights(string command, TasSayEventArgs e) {
            foreach (CommandConfig c in Commands.Commands) {
                if (c.Name == command) {
                    if (c.Throttling > 0) {
                        var diff = (int)DateTime.Now.Subtract(c.lastCall).TotalSeconds;
                        if (diff < c.Throttling) {
                            Respond(e, "AntiSpam - please wait " + (c.Throttling - diff) + " more seconds");
                            return false;
                        }
                    }

                    for (int i = 0; i < c.ListenTo.Length; i++) {
                        if (c.ListenTo[i] == e.Place) {
                            // command is only for nonspecs
                            if (!c.AllowSpecs && !GetUserAdminStatus(e) && GetUserIsSpectator(e)) return false;

                            int reqLevel = c.Level;
                            int ulevel = GetUserLevel(e);

                            if (ulevel >= reqLevel) {
                                c.lastCall = DateTime.Now;
                                return true; // ALL OK
                            }
                            else {
                                Respond(e,
                                    String.Format("Sorry, you do not have rights to execute {0}{1}",
                                        command,
                                        (!string.IsNullOrEmpty(bossName) ? ", ask boss admin " + bossName : "")));
                                    return false;
                            }
                        }
                    }
                    return false; // place not allowed for this command = ignore command
                }
            }
            if (e.Place != SayPlace.Channel) Respond(e, "Sorry, I don't know command '" + command + "'");
            return false;
        }


        public void RegisterVote(TasSayEventArgs e, bool vote) {
            if (activePoll != null) {
                if (activePoll.Vote(e, vote)) {
                    pollTimer.Enabled = false;
                    activePoll = null;
                }
            }
            else Respond(e, "There is no poll going on, start some first");
        }

        public void Respond(TasSayEventArgs e, string text) {
            Respond(tas, spring, e, text);
        }

        public static void Respond(TasClient tas, Spring spring, TasSayEventArgs e, string text) {
            var p = SayPlace.User;
            bool emote = false;
            if (e.Place == SayPlace.Battle) {
                p = SayPlace.BattlePrivate;
                emote = true;
            }
            if (e.Place == SayPlace.Game && spring.IsRunning) spring.SayGame(text);
            else tas.Say(p, e.UserName, text, emote);
        }

        public void RunCommand(string text) {
            string[] allwords = text.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (allwords.Length < 1) return;
            string com = allwords[0];
            // remove first word (command)
            string[] words = ZkData.Utils.ShiftArray(allwords, -1);
            RunCommand(TasSayEventArgs.Default, com, words);
        }

        public void RunCommand(TasSayEventArgs e, string com, string[] words) {
            switch (com) {
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

                case "start":
                    if (tas.MyBattle != null) {
                        int cnt = tas.MyBattle.NonSpectatorCount;
                        if (cnt == 1) ComStart(e, words);
                        else StartVote(new VoteStart(tas, spring, this), e, words);
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
                    linkSpringieClient.FindLinks(words, ResourceLinkSpringieClient.FileType.Map, tas, e);
                    break;

                case "modlink":
                    linkSpringieClient.FindLinks(words, ResourceLinkSpringieClient.FileType.Mod, tas, e);
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

                case "voteresetoptions":
                    StartVote(new VoteResetOptions(tas, spring, this), e, words);
                    break;

                case "predict":
                    ComPredict(e, words);
                    break;

                case "rehost":
                    ComRehost(e, words);
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

                case "team":
                    ComAlly(e, words);
                    break;

                case "resetoptions":
                    ComResetOptions(e, words);
                    break;

                case "helpall":
                    ComHelpAll(e, words);
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
                    if (spring.IsRunning) {
                        spring.SayGame("/cheat");
                        SayBattle("Cheats!");
                    }
                    else Respond(e, "Cannot set cheats, game not running");
                    break;

                case "hostsay":
                    if (spring.IsRunning) spring.SayGame(Utils.Glue(words));
                    else Respond(e, "Game not running");
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
                    ComMove(e, words);
                    break;

                case "votemove":
                    StartVote(new VoteMove(tas, spring, this), e, words);
                    break;

                case "adduser":
                    ComAddUser(e,words);
                    break;


                case "spawn":
                {
                    string args = Utils.Glue(words);
                    if (String.IsNullOrEmpty(args)) {
                        Respond(e, "Please specify parameters");
                        return;
                    }
                    var configKeys = new Dictionary<string, string>();
                    foreach (string f in args.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                        string[] parts = f.Split('=');
                        if (parts.Length == 2) configKeys[parts[0].Trim()] = parts[1].Trim();
                    }
                    var sc = new SpawnConfig(e.UserName, configKeys);
                    if (String.IsNullOrEmpty(sc.Mod)) {
                        Respond(e, "Please specify at least mod name: !spawn mod=zk:stable");
                        return;
                    }
                    Program.main.SpawnAutoHost(config, sc).Start();
                }
                    break;
            }
        }


        public void SayBattle(string text) {
            SayBattle(text, true);
        }

        public void SayBattle(string text, bool ingame) {
            if (!String.IsNullOrEmpty(text)) foreach (string line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) SayBattle(tas, spring, line, ingame);
        }


        public static void SayBattle(TasClient tas, Spring spring, string text, bool ingame) {
            tas.Say(SayPlace.Battle, "", text, true);
            if (spring.IsRunning && ingame) spring.SayGame(text);
        }

        public void SayBattlePrivate(string user, string text) {
            if (!String.IsNullOrEmpty(text)) foreach (string line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) tas.Say(SayPlace.BattlePrivate, user, text, true);
        }

        public void OpenBattleRoom(string modname, string mapname, bool now = true) {
            if (!now && spring.IsRunning) spring.WaitForExit();
            Stop();

            bossName = "";
            lastMapChange = DateTime.Now;

            if (String.IsNullOrEmpty(modname)) modname = config.Mod;
            if (String.IsNullOrEmpty(mapname)) mapname = config.Map;

            string title = config.Title.Replace("%1", MainConfig.SpringieVersion);

            string password = null;
            if (!string.IsNullOrEmpty(config.BattlePassword)) password = config.BattlePassword;

            int maxPlayers = config.MaxPlayers;
            string engine = springPaths.SpringVersion;
            if (SpawnConfig != null) {
                modname = SpawnConfig.Mod;
                if (SpawnConfig.MaxPlayers > 0) maxPlayers = SpawnConfig.MaxPlayers;
                if (!String.IsNullOrEmpty(SpawnConfig.Map)) mapname = SpawnConfig.Map;
                title = SpawnConfig.Title;
                if (!String.IsNullOrEmpty(SpawnConfig.Password)) password = SpawnConfig.Password;
                if (!String.IsNullOrEmpty(SpawnConfig.Engine)) engine = SpawnConfig.Engine;
                {
                    //Something needs to go here to properly tell Springie to use a specific engine version,
                    //attempted code below may or may not be responsible for recent springie drops, so commenting.
                    
                    //the below may be causing a rehost
                    //requestedEngineChange = SpawnConfig.Engine;
                    
                    //alternate attempt
                    /*
                    Program.main.Downloader.GetAndSwitchEngine(SpawnConfig.Engine);
                    config.SpringVersion = SpawnConfig.Engine;
                    springPaths.SetEnginePath(Program.main.paths.GetEngineFolderByVersion(SpawnConfig.Engine));
                    */
                }
            }

            //title = title + string.Format(" [engine{0}]", springPaths.SpringVersion);

            // no mod was provided, auto update is on, check if newer version exists, if it does use that instead of config one
            if (string.IsNullOrEmpty(modname) && !String.IsNullOrEmpty(config.AutoUpdateRapidTag)) {
                var ver = Program.main.Downloader.PackageDownloader.GetByTag(config.AutoUpdateRapidTag);
                
                if (ver != null && cache.GetResourceDataByInternalName(ver.InternalName) != null) modname = config.AutoUpdateRapidTag;
            }

            PackageDownloader.Version version = Program.main.Downloader.PackageDownloader.GetByTag(modname);
            if (version != null && version.InternalName != null) modname = version.InternalName;

            hostedMod = new Mod() {Name = modname};
            cache.GetMod(modname, (m) => { hostedMod = m; }, (m) => {});
            if (hostedMod.IsMission && !String.IsNullOrEmpty(hostedMod.MissionMap)) mapname = hostedMod.MissionMap;

            //Map mapi = null;
            //cache.GetMap(mapname, (m, x, y, z) => { mapi = m; }, (e) => { }, springPaths.SpringVersion);
            //int mint, maxt;
            if (!springPaths.HasEngineVersion(engine)) {
                Program.main.Downloader.GetAndSwitchEngine(engine,springPaths);
            } else {
                springPaths.SetEnginePath(springPaths.GetEngineFolderByVersion(engine));
            }

            var b = new Battle(engine, password, hostingPort, maxPlayers, mapname, title,modname);
            b.Ip = Program.main.Config.IpOverride;
            tas.OpenBattle(b);
        }


        public void StartVote(IVotable vote, TasSayEventArgs e, string[] words) {
            if (vote != null) {
                if (activePoll != null) {
                    Respond(e, "Another poll already in progress, please wait");
                    return;
                }
                if (vote.Setup(e, words)) {
                    activePoll = vote;
                    pollTimer.Interval = PollTimeout*1000;
                    pollTimer.Enabled = true;
                }
            }
        }


        public void Stop() {
            StopVote();
            spring.ExitGame();
            tas.ChangeMyUserStatus(false, false);
            bossName = "";
            tas.LeaveBattle();
        }

        public void StopVote() {
            if (activePoll != null) activePoll.End();
            if (pollTimer != null) pollTimer.Enabled = false;
            activePoll = null;
        }

        void CheckEngineBranch() {
            string url = String.Format("http://springrts.com/dl/buildbot/default/{0}/LATEST", config.AutoUpdateSpringBranch);
            try {
                var wc = new WebClient();
                string str = wc.DownloadString(url);
                string bstr = "{" + config.AutoUpdateSpringBranch + "}";
                if (str.StartsWith(bstr)) str = str.Replace(bstr, "");
                str = str.Trim('\n', '\r', ' ');

                if (springPaths.SpringVersion != str) ComSetEngine(TasSayEventArgs.Default, new[] { str });
            } catch (Exception ex) {
                Trace.TraceWarning("Error getting latest engine branch version from {0}: {1}");
            }
        }


        void CheckForBattleExit() {
            if ((DateTime.Now - spring.GameStarted) > TimeSpan.FromSeconds(20)) {
                /*if (spring.IsRunning && !spring.IsBattleOver)
                { // don't exit here if game is already over; leave it to the timed exit thread in spring_GameOver
                    Battle b = tas.MyBattle;
                    int count = 0;
                    foreach (UserBattleStatus p in b.Users) {
                        if (p.IsSpectator) continue;

                        User u;
                        if (!tas.GetExistingUser(p.Name, out u)) continue;
                        if (u.IsInGame) count++;
                    }
                    if (count < 1) {
                        SayBattle("closing game, " + count + " active player left in game");
                        spring.ExitGame();
                    }
                }*/ 
                // kontrola pro pripad ze by se nevypl spring
                User us;
                if (!spring.IsRunning && tas.GetExistingUser(tas.UserName, out us) && us.IsInGame) tas.ChangeMyUserStatus(false, false);
            }
        }

        /// <summary>
        ///     Gets free slots, first mandatory then optional
        /// </summary>
        /// <returns></returns>
        IEnumerable<MissionSlot> GetFreeSlots() {
            Battle b = tas.MyBattle;
            return
                hostedMod.MissionSlots.Where(x => x.IsHuman)
                         .OrderByDescending(x => x.IsRequired)
                         .Where(x => !b.Users.Values.Any(y => y.AllyNumber == x.AllyID && y.TeamNumber == x.TeamID && !y.IsSpectator));
        }


        void UpdateRapidMod(string tag) {
            if (!string.IsNullOrEmpty(delayedModChange) && !spring.IsRunning) {
                string latest = delayedModChange;
                delayedModChange = null;
                config.Mod = latest;
                SayBattle("Updating to latest mod version: " + latest);
                if (tas.MyBattle != null) ComRehost(TasSayEventArgs.Default, new[] { latest });
            }
            else {
                PackageDownloader.Version version = Program.main.Downloader.PackageDownloader.GetByTag(tag);
                if (version != null) {
                    string latest = version.InternalName;
                    if (!String.IsNullOrEmpty(latest) && (tas.MyBattle == null || tas.MyBattle.ModName != latest)) {
                        if (cache.GetResourceDataByInternalName(latest) != null && !spring.IsRunning) {
                            config.Mod = latest;
                            SayBattle("Updating to latest mod version: " + latest);
                            if (tas.MyBattle != null) ComRehost(TasSayEventArgs.Default, new[] { latest });
                        }
                        else delayedModChange = latest;
                    }
                }
            }
        }


        void Downloader_PackagesChanged(object sender, EventArgs e) {
            if (!string.IsNullOrEmpty(config.AutoUpdateRapidTag) && SpawnConfig == null) UpdateRapidMod(config.AutoUpdateRapidTag);
        }


        void pollTimer_Elapsed(object sender, ElapsedEventArgs e) {
            try {
                pollTimer.Stop();
                if (activePoll != null) activePoll.End();
                StopVote();
            } catch {} finally {
                pollTimer.Start();
            }
        }

        void spring_BattleStarted(object sender, EventArgs e) {
            StopVote();
        }

        void spring_GameOver(object sender, SpringLogEventArgs e) {
            SayBattle("Game over, exiting");
            // Spring sends GAMEOVER for every player and spec, we only need the first one.
            spring.GameOver -= spring_GameOver;
            ZkData.Utils.SafeThread(() =>
                {
                    // Wait for gadgets that send spring autohost messages after gadget:GameOver()
                    // such as awards.lua
                    Thread.Sleep(10000);
                    spring.ExitGame();
                    spring.GameOver += spring_GameOver;
                }).Start();
        }


        void spring_PlayerSaid(object sender, SpringLogEventArgs e) {
            tas.GameSaid(e.Username, e.Line);
            User us;
            tas.ExistingUsers.TryGetValue(e.Username, out us);
            bool isMuted = us != null && us.BanMute;
            if (Program.main.Config.RedirectGameChat && e.Username != tas.UserName && !e.Line.StartsWith("Allies:") &&
                !e.Line.StartsWith("Spectators:") && !isMuted) tas.Say(SayPlace.Battle, "", "[" + e.Username + "]" + e.Line, false);
        }


        void spring_SpringExited(object sender, EventArgs e) {
            StopVote();
            tas.ChangeMyUserStatus(false, false);
            Battle b = tas.MyBattle;
            foreach (string s in toNotify) {
                if (b != null && b.Users.ContainsKey(s)) tas.Ring(SayPlace.BattlePrivate, s);
                tas.Say(SayPlace.User, s, "** Game just ended, join me! **", false);
            }
            toNotify.Clear();

            if (SpawnConfig == null && DateTime.Now.Subtract(spring.GameStarted).TotalMinutes > 5) ServerVerifyMap(true);
        }

        void spring_SpringStarted(object sender, EventArgs e)
        {
            //lockedUntil = DateTime.MinValue;
            //tas.ChangeLock(false);
            if (hostedMod.IsMission) {
                var service = GlobalConst.GetContentService();
                foreach (UserBattleStatus u in tas.MyBattle.Users.Values.Where(x => !x.IsSpectator)) service.NotifyMissionRun(u.Name, hostedMod.ShortName);
            }
        
            StopVote();
        }


        void tas_BattleOpened(object sender, Battle battle) {
            tas.ChangeMyBattleStatus(true, SyncStatuses.Synced);
            if (hostedMod.IsMission) {
                foreach (MissionSlot slot in hostedMod.MissionSlots.Where(x => !x.IsHuman)) {
                    tas.AddBot(slot.TeamName, slot.AiShortName, slot.AllyID, slot.TeamID);
                }
            }

            tas_MyBattleMapChanged(this, null); // todo really hacky thing

            if (SpawnConfig != null)
            {
                if (!string.IsNullOrEmpty(SpawnConfig.Handle)) tas.Say(SayPlace.User, SpawnConfig.Owner, SpawnConfig.Handle, true);
                    tas.Say(SayPlace.User, SpawnConfig.Owner, "I'm here! Ready to serve you! Join me!", true);
            }
            else ServerVerifyMap(true);


            
        }


        void tas_BattleUserJoined(object sender, BattleUserEventArgs e1) {
            if (e1.BattleID != tas.MyBattleID) return;
            string name = e1.UserName;

            string welc = config.Welcome;
            if (!string.IsNullOrEmpty(welc)) {
                welc = welc.Replace("%1", name);
                welc = welc.Replace("%2", GetUserLevel(name).ToString());
                welc = welc.Replace("%3", MainConfig.SpringieVersion);
                SayBattlePrivate(name, welc);
            }
            if (spring.IsRunning) {
                spring.AddUser(e1.UserName, e1.ScriptPassword);
                TimeSpan started = DateTime.Now.Subtract(spring.GameStarted);
                started = new TimeSpan((int)started.TotalHours, started.Minutes, started.Seconds);
                SayBattlePrivate(name, String.Format("THIS GAME IS CURRENTLY IN PROGRESS, PLEASE WAIT UNTIL IT ENDS! Running for {0}", started));
                SayBattlePrivate(name, "If you say !notify, I will message you when the current game ends.");
            }

            if (SpawnConfig == null) {
                try {
                    var serv = GlobalConst.GetSpringieService();
                    PlayerJoinResult ret = serv.AutohostPlayerJoined(tas.MyBattle.GetContext(), tas.ExistingUsers[name].AccountID);
                    if (ret != null) {
                        if (!string.IsNullOrEmpty(ret.PrivateMessage)) tas.Say(SayPlace.User, name, ret.PrivateMessage, false);
                        if (!string.IsNullOrEmpty(ret.PublicMessage)) tas.Say(SayPlace.Battle, "", ret.PublicMessage, true);
                        if (ret.ForceSpec) tas.ForceSpectator(name);
                        if (ret.Kick) tas.Kick(name);
                    }
                } catch (Exception ex) {
                    SayBattle("ServerManage error: " + ex, false);
                }
            }

            if (SpawnConfig != null && SpawnConfig.Owner == name) // owner joins, set him boss 
                ComBoss(TasSayEventArgs.Default, new[] { name });

        }

        void tas_BattleUserLeft(object sender, BattleUserEventArgs e1) {
            if (e1.BattleID != tas.MyBattleID) return;
            CheckForBattleExit();

            if (spring.IsRunning) spring.SayGame(e1.UserName + " has left the room");

            if (e1.UserName == bossName) {
                SayBattle("boss has left the battle");
                bossName = "";
            }
        }


        // login accepted - join channels

        // im connected, let's login
        void tas_Connected(object sender, Welcome welcome) {
            tas.Login(GetAccountName(), config.Password);
        }


        void tas_ConnectionLost(object sender, TasEventArgs e) {
            Stop();
        }


        void tas_LoginAccepted(object sender, TasEventArgs e) {
            foreach (string c in config.JoinChannels) tas.JoinChannel(c);
        }

        void tas_LoginDenied(object sender, LoginResponse loginResponse) {
            if (loginResponse.ResultCode == LoginResponse.Code.InvalidName) tas.Register(GetAccountName(), config.Password);
            else {
                CloneNumber++;
                tas.Login(GetAccountName(), config.Password);
            }
        }

        public DateTime lastMapChange = DateTime.Now;

        void tas_MyBattleMapChanged(object sender, OldNewPair<Battle> oldNewPair) {
            lastMapChange = DateTime.Now;

            Battle b = tas.MyBattle;
            if (b != null) {
                string mapName = b.MapName.ToLower();

                if (SpawnConfig == null) {
                    ComResetOptions(TasSayEventArgs.Default, new string[] { });
                    ComClearBox(TasSayEventArgs.Default, new string[] { });
                }

                try {
                    var serv = GlobalConst.GetSpringieService();
                    string commands = serv.GetMapCommands(mapName);
                    if (!string.IsNullOrEmpty(commands)) foreach (string c in commands.Split('\n').Where(x => !string.IsNullOrEmpty(x))) RunCommand(c);
                } catch (Exception ex) {
                    Trace.TraceError("Error procesing map commands: {0}", ex);
                }
            }
        }

        public BattleContext slaveContextOverride;

        public override string ToString()
        {
            return string.Format("[{0}]",tas.UserName);
        }

        void tas_MyStatusChangedToInGame(object sender, Battle battle) {
            spring.HostGame(tas.MyBattle.GetContext(), battle.Ip, battle.HostPort);
        }

        void tas_Said(object sender, TasSayEventArgs e) {
            if (e.Place == SayPlace.MessageBox) Trace.TraceInformation("{0} server message: {1}", this, e.Text);

            if (String.IsNullOrEmpty(e.UserName)) return;
            if (Program.main.Config.RedirectGameChat && e.Place == SayPlace.Battle &&
                e.UserName != tas.UserName && e.IsEmote == false && !tas.ExistingUsers[e.UserName].BanMute) spring.SayGame(string.Format("<{0}>{1}", e.UserName, e.Text));
            
            // check if it's command
            if (!e.IsEmote && e.Text.StartsWith("!")) {
                if (e.Text.Length < 2) return;
                string[] allwords = e.Text.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (allwords.Length < 1) return;
                string com = allwords[0];

                // remove first word (command)
                string[] words = ZkData.Utils.ShiftArray(allwords, -1);

                
                if (!HasRights(com, e)) {
                    if (!com.StartsWith("vote")) {
                        com = "vote" + com;

                        if (!Commands.Commands.Any(x => x.Name == com) || !HasRights(com, e)) return;
                    }
                    else return;
                }
               

                if (e.Place == SayPlace.User) {
                    if (com != "say" && com != "admins" && com != "help" && com != "helpall" && com != "springie" && com != "listoptions" &&
                        com != "spawn" && com != "predict" && com != "notify" && com != "transmit" && com != "adduser") SayBattle(String.Format("{0} executed by {1}", com, e.UserName));
                }

                RunCommand(e, com, words);
            }
        }


        void tas_UserStatusChanged(object sender, OldNewPair<User> oldNewPair) {
            if (spring.IsRunning) {
                Battle b = tas.MyBattle;
                var name = oldNewPair.New.Name;
                if (name != tas.UserName && b.Users.ContainsKey(name)) CheckForBattleExit();
            }
            
        }
    }
}
