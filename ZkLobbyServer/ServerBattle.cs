using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Timers;
using LobbyClient;
using PlasmaShared;
using Springie;
using Springie.autohost;
using Springie.autohost.Polls;
using ZkData;
using ZkData.UnitSyncLib;
using Timer = System.Timers.Timer;
using Utils = Springie.Utils;

namespace ZkLobbyServer
{
    public partial class ServerBattle:Battle
    {
        public const int PollTimeout = 60;
        public readonly CommandList Commands;

        public ZkLobbyServer server;

        IVotable activePoll;
        Timer pollTimer;

        //static Downloader


        public Mod hostedMod;
        public int hostingPort { get; private set; }
        public Spring spring;

        public AutohostMode mode => this.Mode;

        private SpringPaths springPaths;

        public ServerBattle(ZkLobbyServer server)
        {
            this.server = server;
            Commands = new CommandList();
            this.hostingPort = hostingPort;

            springPaths = new SpringPaths(GlobalConst.SpringieDataDir, false);



            pollTimer = new Timer(PollTimeout * 1000);
            pollTimer.Enabled = false;
            pollTimer.AutoReset = false;
            pollTimer.Elapsed += pollTimer_Elapsed;


            lastMapChange = DateTime.Now;

     
            SetupSpring();

        }

        public void FillDetails()
        {
            if (string.IsNullOrEmpty(Title)) Title = $"{FounderName}'s game";
            if (string.IsNullOrEmpty(EngineName)) EngineName = server.Engine;
            

            switch (Mode)
            {
                case AutohostMode.Game1v1:
                    MaxPlayers = 2;
                    break;
                case AutohostMode.Planetwars:
                    MaxPlayers = 4;
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

            


            //if (String.IsNullOrEmpty(modname)) modname = config.Mod;
            //if (String.IsNullOrEmpty(mapname)) mapname = config.Map;

           
            // no mod was provided, auto update is on, check if newer version exists, if it does use that instead of config one
            /*if (string.IsNullOrEmpty(modname) && !String.IsNullOrEmpty(config.AutoUpdateRapidTag))
            {
                var ver = Program.main.Downloader.PackageDownloader.GetByTag(config.AutoUpdateRapidTag);

                if (ver != null && cache.GetResourceDataByInternalName(ver.InternalName) != null) modname = config.AutoUpdateRapidTag;
            }

            PackageDownloader.Version version = Program.main.Downloader.PackageDownloader.GetByTag(modname);
            if (version != null && version.InternalName != null) modname = version.InternalName;

            hostedMod = new Mod() { Name = modname };
            cache.GetMod(modname, (m) => { hostedMod = m; }, (m) => { });
            if (hostedMod.IsMission && !String.IsNullOrEmpty(hostedMod.MissionMap)) mapname = hostedMod.MissionMap;

            //Map mapi = null;
            //cache.GetMap(mapname, (m, x, y, z) => { mapi = m; }, (e) => { }, springPaths.SpringVersion);
            //int mint, maxt;
            if (!springPaths.HasEngineVersion(engine))
            {
                Program.main.Downloader.GetAndSwitchEngine(engine, springPaths);
            }
            else
            {
                springPaths.SetEnginePath(springPaths.GetEngineFolderByVersion(engine));
            }

            var b = new Battle(engine, password, hostingPort, maxPlayers, mapname, title, modname);
            b.Ip = Program.main.Config.IpOverride;
            tas.OpenBattle(b);*/

        }



        void SetupSpring()
        {
            spring?.UnsubscribeEvents(this);

            spring = new Spring(springPaths) { UseDedicatedServer = true };

            spring.SpringExited += spring_SpringExited;
            spring.GameOver += spring_GameOver;

            spring.SpringStarted += spring_SpringStarted;
            spring.PlayerSaid += spring_PlayerSaid;
            spring.BattleStarted += spring_BattleStarted;
        }

        
        public void Dispose()
        {
            Stop();
            spring.UnsubscribeEvents(this);
            springPaths.UnsubscribeEvents(this);
            //Program.Downloader.UnsubscribeEvents(this);
            //Program.paths.UnsubscribeEvents(this);
            pollTimer.Dispose();
            pollTimer = null;
        }


        /*void fileDownloader_DownloadProgressChanged(object sender, TasEventArgs e)
    {
      if (tas.IsConnected) {
        SayBattle(e.ServerParams[0] + " " + e.ServerParams[1] + "% done");
      }
    }*/

        public bool GetUserAdminStatus(TasSayEventArgs e)
        {
            if (!server.ConnectedUsers.ContainsKey(e.UserName)) return false;
            if (FounderName == e.UserName) return true;
            return server.ConnectedUsers[e.UserName].User.IsAdmin;
        }

        public bool GetUserIsSpectator(TasSayEventArgs e)
        {
            if (spring.IsRunning)
            {
                PlayerTeam user = spring.StartContext.Players.FirstOrDefault(x => x.Name == e.UserName && !x.IsSpectator);
                return ((user == null) || user.IsSpectator);
            }
            else
            {
                return !Users.Values.Any(x => x.LobbyUser.Name == e.UserName && !x.IsSpectator);
            }
        }

        public int GetUserLevel(TasSayEventArgs e)
        {
            if (!server.ConnectedUsers.ContainsKey(e.UserName))
            {
                //Respond(e, string.Format("{0} please reconnect to lobby for right verification", e.UserName));
                return 0; //1 is default, but we return 0 to avoid right abuse (by Disconnecting from Springie and say thru Spring)
            }
            return GetUserLevel(e.UserName);
        }

        public int GetUserLevel(string name)
        {
            int ret = server.ConnectedUsers[name].User.SpringieLevel;
            if (name == FounderName) ret = Math.Max(GlobalConst.SpringieBossEffectiveRights, ret);
            else ret += -1;
            return ret;
        }


        public bool HasRights(string command, TasSayEventArgs e, bool hideRightsMessage = false)
        {
            foreach (CommandConfig c in Commands.Commands)
            {
                if (!c.ListenTo.Contains(e.Place)) continue;
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

                    for (int i = 0; i < c.ListenTo.Length; i++)
                    {
                        if (c.ListenTo[i] == e.Place)
                        {
                            // command is only for nonspecs
                            if (!c.AllowSpecs && !GetUserAdminStatus(e) && GetUserIsSpectator(e)) return false;

                            int reqLevel = c.Level;
                            int ulevel = GetUserLevel(e);

                            if (ulevel >= reqLevel)
                            {
                                c.lastCall = DateTime.Now;
                                return true; // ALL OK
                            }
                            else
                            {
                                if (!hideRightsMessage)
                                {
                                    Respond(e, $"Sorry, you do not have rights to execute {command}");
                                }
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


        public void RegisterVote(TasSayEventArgs e, bool vote)
        {
            if (activePoll != null)
            {
                if (activePoll.Vote(e, vote))
                {
                    pollTimer.Enabled = false;
                    activePoll = null;
                }
            }
            else Respond(e, "There is no poll going on, start some first");
        }

        public void Respond(TasSayEventArgs e, string text)
        {
            Respond(spring, e, text);
        }

        public void Respond(Spring spring, TasSayEventArgs e, string text)
        {
            var p = SayPlace.User;
            bool emote = false;
            if (e.Place == SayPlace.Battle)
            {
                p = SayPlace.BattlePrivate;
                emote = true;
            }
            if (e.Place == SayPlace.Game && spring.IsRunning) spring.SayGame(text);
            else this.server.GhostSay(new Say() { Place = SayPlace.Battle, AllowRelay = false, IsEmote = false, Ring = false, Target = null, Text = text });
                
        }

        public void RunCommand(string text)
        {
            string[] allwords = text.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (allwords.Length < 1) return;
            string com = allwords[0];
            // remove first word (command)
            string[] words = ZkData.Utils.ShiftArray(allwords, -1);
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

                case "mapremote":
                    ComMapRemote(e, words);
                    break;

                case "start":
                        int cnt = NonSpectatorCount;
                        if (cnt == 1) ComStart(e, words);
                        else StartVote(new VoteStart(spring, this), e, words);

                    break;

                case "forcestart":
                    ComForceStart(e, words);
                    break;

                case "force":
                    ComForce(e, words);
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
                    StartVote(new VoteMap(spring, this), e, words);
                    break;

                case "votekick":
                    StartVote(new VoteKick(spring, this), e, words);
                    break;

                case "votespec":
                    StartVote(new VoteSpec(spring, this), e, words);
                    break;

                case "voteresign":
                    StartVote(new VoteResign(spring, this), e, words);
                    break;

                case "voteforcestart":
                    StartVote(new VoteForceStart(spring, this), e, words);
                    break;

                case "voteforce":
                    StartVote(new VoteForce(spring, this), e, words);
                    break;

                case "voteexit":
                    StartVote(new VoteExit(spring, this), e, words);
                    break;

                case "voteresetoptions":
                    StartVote(new VoteResetOptions(spring, this), e, words);
                    break;

                case "predict":
                    ComPredict(e, words);
                    break;

                case "rehost":
                    ComRehost(e, words);
                    break;


                case "balance":
                    ComBalance(e, words);
                    break;

                case "resetoptions":
                    ComResetOptions(e, words);
                    break;


                case "endvote":
                    StopVote(e);
                    SayBattle("poll cancelled");
                    break;


                case "cbalance":
                    ComCBalance(e, words);
                    break;

                case "notify":
                    ComNotify(e, words);
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


                case "cheats":
                    if (spring.IsRunning)
                    {
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
                    StartVote(new VoteSetOptions(spring, this), e, words);
                    break;

                case "setengine":
                    ComSetEngine(e, words);
                    break;

                case "transmit":
                    ComTransmit(e, words);
                    break;


                case "adduser":
                    ComAddUser(e, words);
                    break;


            }
        }


        public void SayBattle(string text)
        {
            SayBattle(text, true);
        }

        public void SayBattle(string text, bool ingame)
        {
            if (!String.IsNullOrEmpty(text)) foreach (string line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) SayBattle(spring, line, ingame);
        }


        public static void SayBattle(Spring spring, string text, bool ingame)
        {
            throw new NotImplementedException();
            //tas.Say(SayPlace.Battle, "", text, true);
            if (spring != null && spring.IsRunning && ingame) spring.SayGame(text);
        }

        public void SayBattlePrivate(string user, string text)
        {
            throw new NotImplementedException();
            //if (!String.IsNullOrEmpty(text)) foreach (string line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) tas.Say(SayPlace.BattlePrivate, user, text, true);
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
                    pollTimer.Interval = PollTimeout * 1000;
                    pollTimer.Enabled = true;
                }
            }
        }


        public void Stop()
        {
            StopVote();
        }

        public void StopVote(TasSayEventArgs e = null)
        {
            if (e != null)
            {
                string name = e.UserName;
                if (name != null && activePoll != null && name != activePoll.Creator)
                {
                    if (GetUserLevel(name) < GlobalConst.SpringieBossEffectiveRights)
                    {
                        Respond(e, "Sorry, you do not have rights to end this vote");
                        return;
                    }
                }
            }
            if (activePoll != null) activePoll.End();
            if (pollTimer != null) pollTimer.Enabled = false;
            activePoll = null;
        }

        

        /// <summary>
        ///     Gets free slots, first mandatory then optional
        /// </summary>
        /// <returns></returns>
        IEnumerable<MissionSlot> GetFreeSlots()
        {
            Battle b = this;
            return
                hostedMod.MissionSlots.Where(x => x.IsHuman)
                         .OrderByDescending(x => x.IsRequired)
                         .Where(x => !b.Users.Values.Any(y => y.AllyNumber == x.AllyID && y.TeamNumber == x.TeamID && !y.IsSpectator));
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
            finally
            {
                pollTimer.Start();
            }
        }

        void spring_BattleStarted(object sender, EventArgs e)
        {
            StopVote();
        }

        void spring_GameOver(object sender, SpringLogEventArgs e)
        {
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


        void spring_PlayerSaid(object sender, SpringLogEventArgs e)
        {
            /*tas.GameSaid(e.Username, e.Line);
            User us;
            tas.ExistingUsers.TryGetValue(e.Username, out us);
            bool isMuted = us != null && us.BanMute;
            if (Program.main.Config.RedirectGameChat && e.Username != tas.UserName && !e.Line.StartsWith("Allies:") &&
                !e.Line.StartsWith("Spectators:") && !isMuted) tas.Say(SayPlace.Battle, "", "[" + e.Username + "]" + e.Line, false);*/
        }


        void spring_SpringExited(object sender, EventArgs e)
        {
            StopVote();
            /*tas.ChangeMyUserStatus(false, false);
            Battle b = tas.MyBattle;
            foreach (string s in toNotify)
            {
                if (b != null && b.Users.ContainsKey(s)) tas.Ring(SayPlace.BattlePrivate, s);
                tas.Say(SayPlace.User, s, "** Game just ended, join me! **", false);
            }
            toNotify.Clear();
            */
            //if (mode != AutohostMode.None && DateTime.Now.Subtract(spring.GameStarted).TotalMinutes > 5) ServerVerifyMap(true);
            ComMap(TasSayEventArgs.Default);
        }

        void spring_SpringStarted(object sender, EventArgs e)
        {
            //lockedUntil = DateTime.MinValue;
            //tas.ChangeLock(false);
            if (hostedMod.IsMission)
            {
                var service = GlobalConst.GetContentService();
                foreach (UserBattleStatus u in Users.Values.Where(x => !x.IsSpectator)) service.NotifyMissionRun(u.Name, hostedMod.ShortName);
            }

            StopVote();
        }


        void tas_BattleOpened(object sender, Battle battle)
        {
            /*tas.ChangeMyBattleStatus(true, SyncStatuses.Synced);
            if (hostedMod.IsMission)
            {
                foreach (MissionSlot slot in hostedMod.MissionSlots.Where(x => !x.IsHuman))
                {
                    tas.AddBot(slot.TeamName, slot.AiShortName, slot.AllyID, slot.TeamID);
                }
            }

            tas_MyBattleMapChanged(this, null); // todo really hacky thing

            if (SpawnConfig != null)
            {
                if (!string.IsNullOrEmpty(SpawnConfig.Handle)) tas.Say(SayPlace.User, SpawnConfig.Owner, SpawnConfig.Handle, true);
                tas.Say(SayPlace.User, SpawnConfig.Owner, "I'm here! Ready to serve you! Join me!", true);
            }

            if (!hostedMod.IsMission) ServerVerifyMap(true);*/
        }


        void tas_BattleUserJoined(object sender, BattleUserEventArgs e1)
        {
            /*
            if (e1.BattleID != tas.MyBattleID) return;
            string name = e1.UserName;

            kickedPlayers.RemoveAll(x => x.TimeOfKicked <= DateTime.UtcNow.AddMinutes(-5));
            if (kickedPlayers.Any(y => y.Name == name)) ComKick(TasSayEventArgs.Default, new[] { name });



            string welc = config.Welcome;
            if (!string.IsNullOrEmpty(welc))
            {
                welc = welc.Replace("%1", name);
                welc = welc.Replace("%2", GetUserLevel(name).ToString());
                welc = welc.Replace("%3", MainConfig.SpringieVersion);

                string[] split = welc.Split(new Char[] { '\n' });
                foreach (string s in split)
                {
                    SayBattlePrivate(name, s);
                }
            }
            if (spring.IsRunning)
            {
                spring.AddUser(e1.UserName, e1.ScriptPassword);
                TimeSpan started = DateTime.Now.Subtract(spring.GameStarted);
                started = new TimeSpan((int)started.TotalHours, started.Minutes, started.Seconds);
                SayBattlePrivate(name, String.Format("THIS GAME IS CURRENTLY IN PROGRESS, PLEASE WAIT UNTIL IT ENDS! Running for {0}", started));
                SayBattlePrivate(name, "If you say !notify, I will message you when the current game ends.");
            }

            if (SpawnConfig == null)
            {
                try
                {
                    var serv = GlobalConst.GetSpringieService();
                    PlayerJoinResult ret = serv.AutohostPlayerJoined(tas.MyBattle.GetContext(), tas.ExistingUsers[name].AccountID);
                    if (ret != null)
                    {
                        if (!string.IsNullOrEmpty(ret.PrivateMessage)) tas.Say(SayPlace.User, name, ret.PrivateMessage, false);
                        if (!string.IsNullOrEmpty(ret.PublicMessage)) tas.Say(SayPlace.Battle, "", ret.PublicMessage, true);
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
                */

        }

        void tas_BattleUserLeft(object sender, BattleUserEventArgs e1)
        {
            /*
            if (e1.BattleID != tas.MyBattleID) return;
            CheckForBattleExit();

            if (spring.IsRunning) spring.SayGame(e1.UserName + " has left the room");

            if (e1.UserName == bossName)
            {
                SayBattle("boss has left the battle");
                bossName = "";
            }

            // repick map after everyone (except the autohost) leaves to prevent someone from setting trololo everywhere
            if (tas.MyBattle != null && tas.MyBattle.Users.Count == 1) ServerVerifyMap(true);*/
        }

        // login accepted - join channels

        public DateTime lastMapChange = DateTime.Now;

        void tas_MyBattleMapChanged(object sender, OldNewPair<Battle> oldNewPair)
        {
            /*
            lastMapChange = DateTime.Now;

            Battle b = tas.MyBattle;
            if (b != null)
            {
                string mapName = b.MapName.ToLower();

                if (SpawnConfig == null)
                {
                    ComResetOptions(TasSayEventArgs.Default, new string[] { });
                    ComClearBox(TasSayEventArgs.Default, new string[] { });
                }

                try
                {
                    var serv = GlobalConst.GetSpringieService();
                    string commands = serv.GetMapCommands(mapName);
                    if (!string.IsNullOrEmpty(commands)) foreach (string c in commands.Split('\n').Where(x => !string.IsNullOrEmpty(x))) RunCommand(c);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error procesing map commands: {0}", ex);
                }
            }*/
        }


        void tas_MyStatusChangedToInGame(object sender, Battle battle)
        {
            SetupSpring();
            //spring.lobbyUserName = tas.UserName; // hack until removed when springie moves to server
            //spring.lobbyPassword = tas.UserPassword;  // spring class needs this to submit results

            //spring.HostGame(GetContext(), "127.0.0.1", 8452);  // TODO HACK GET PORTS
        }

        public ConnectedUser FounderUser
        {
            get
            {
                ConnectedUser connectedUser = null;
                server.ConnectedUsers.TryGetValue(FounderName, out connectedUser);
                return connectedUser;
            }
        }

        void tas_Said(object sender, TasSayEventArgs e)
        {
            if (e.Place == SayPlace.MessageBox) Trace.TraceInformation("{0} server message: {1}", this, e.Text);

            if (String.IsNullOrEmpty(e.UserName)) return;
            if (e.Place == SayPlace.Battle &&  e.IsEmote == false && !server.ConnectedUsers[e.UserName].User.BanMute && !server.ConnectedUsers[e.UserName].User.BanSpecChat) spring.SayGame(string.Format("<{0}>{1}", e.UserName, e.Text));

            // check if it's command
            if (!e.IsEmote && e.Text.StartsWith("!"))
            {
                if (e.Text.Length < 2) return;
                string[] allwords = e.Text.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (allwords.Length < 1) return;
                string com = allwords[0];

                // remove first word (command)
                string[] words = ZkData.Utils.ShiftArray(allwords, -1);

                string voteCom = "vote" + com;
                bool hasVoteVersion = !com.StartsWith("vote") && Commands.Commands.Any(x => x.Name == voteCom);

                if (!HasRights(com, e))
                {
                    if (hasVoteVersion)
                    {
                        com = voteCom;
                        if (!HasRights(voteCom, e)) return;
                    }
                    else return;
                }


                if (e.Place == SayPlace.User)
                {
                    if (com != "say" && com != "admins" && com != "help" && com != "helpall" && com != "springie" && com != "listoptions" &&
                        com != "spawn" && com != "predict" && com != "notify" && com != "transmit" && com != "adduser") SayBattle(String.Format("{0} executed by {1}", com, e.UserName));
                }

                RunCommand(e, com, words);
            }
        }


        public void ProcessBattleCommand(string text)
        {


        }

        private void StartGame()
        {
            throw new NotImplementedException();
        }
    }
}