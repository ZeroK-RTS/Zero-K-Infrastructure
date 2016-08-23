using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using PlasmaShared;
using Springie;
using Springie.autohost;
using Springie.autohost.Polls;
using ZeroKWeb.SpringieInterface;
using ZkData;
using ZkData.UnitSyncLib;
using static System.String;
using Timer = System.Timers.Timer;
using Utils = Springie.Utils;

namespace ZkLobbyServer
{
    public partial class ServerBattle:Battle
    {
        private static PlasmaDownloader.PlasmaDownloader downloader;
        private static SpringPaths springPaths;


        public const int PollTimeout = 60;
        public readonly CommandList Commands;

        public ZkLobbyServer server;

        IVotable activePoll;
        Timer pollTimer;

        public Mod hostedMod;
        public Spring spring;

        internal static Say defaultSay = new Say() { Place = SayPlace.Battle, User = "NightWatch"};

        static ServerBattle()
        {
            springPaths = new SpringPaths(GlobalConst.SpringieDataDir, false);
            downloader = new PlasmaDownloader.PlasmaDownloader(null, springPaths);
            downloader.PackageDownloader.SetMasterRefreshTimer(60);
            downloader.PackageDownloader.LoadMasterAndVersions(false);
            downloader.GetEngine(GlobalConst.DefaultEngineOverride);
        }

        public ServerBattle(ZkLobbyServer server)
        {
            this.server = server;
            Commands = new CommandList();
           
            pollTimer = new Timer(PollTimeout * 1000);
            pollTimer.Enabled = false;
            pollTimer.AutoReset = false;
            pollTimer.Elapsed += pollTimer_Elapsed;


            lastMapChange = DateTime.Now;

            SetupSpring();
        }

        public void FillDetails()
        {
            if (IsNullOrEmpty(Title)) Title = $"{FounderName}'s game";
            if (IsNullOrEmpty(EngineVersion)) EngineVersion = server.Engine;
            downloader.GetEngine(server.Engine);
            
            
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

            if (IsNullOrEmpty(ModName)) ModName = "zk:stable";
            ModName = downloader.PackageDownloader.GetByTag(ModName)?.InternalName ?? ModName; // resolve rapid

            if (IsNullOrEmpty(MapName)) MapName = MapPicker.GetRecommendedMap(GetContext())?.InternalName ?? "Small_Divide-Remake-v04";
        }


        public override void UpdateWith(BattleHeader h)
        {
            base.UpdateWith(h);
            RunningSince = null;  // todo hook to spring
            FillDetails();
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

        public bool GetUserAdminStatus(Say e)
        {
            if (!server.ConnectedUsers.ContainsKey(e.User)) return false;
            if (FounderName == e.User) return true;
            return server.ConnectedUsers[e.User].User.IsAdmin;
        }

        public bool GetUserIsSpectator(Say e)
        {
            if (spring.IsRunning)
            {
                PlayerTeam user = spring.StartContext.Players.FirstOrDefault(x => x.Name == e.User && !x.IsSpectator);
                return ((user == null) || user.IsSpectator);
            }
            else
            {
                return !Users.Values.Any(x => x.LobbyUser.Name == e.User && !x.IsSpectator);
            }
        }

        public int GetUserLevel(Say e)
        {
            if (!server.ConnectedUsers.ContainsKey(e.User))
            {
                //Respond(e, string.Format("{0} please reconnect to lobby for right verification", e.UserName));
                return 0; //1 is default, but we return 0 to avoid right abuse (by Disconnecting from Springie and say thru Spring)
            }
            return GetUserLevel(e.User);
        }

        public int GetUserLevel(string name)
        {
            int ret = server.ConnectedUsers[name].User.SpringieLevel;
            if (name == FounderName) ret = Math.Max(GlobalConst.SpringieBossEffectiveRights, ret);
            else ret += -1;
            return ret;
        }


        public bool HasRights(string command, Say e, bool hideRightsMessage = false)
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


        public void RegisterVote(Say e, bool vote)
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


        public Task Respond(Say e, string text)
        {
            return SayBattle(text, e.User);
        }


        public async Task RunCommand(Say e, string com, string[] words)
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
                    await ComMap(e, words);
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

        
        public async Task SayBattle(string text, string privateUser = null)
        {
            if (!IsNullOrEmpty(text))
                foreach (string line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (privateUser == null && spring?.IsRunning == true) spring.SayGame(line);
                    await server.GhostSay(new Say()
                    {
                        User = GlobalConst.NightwatchName,
                        Text = line,
                        Place = privateUser != null ? SayPlace.BattlePrivate : SayPlace.Battle,
                        Target = privateUser,
                        IsEmote = true
                    }, BattleID);
                }
        }


        public void StartVote(IVotable vote, Say e, string[] words)
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

        public void StopVote(Say e = null)
        {
            if (e != null)
            {
                string name = e.User;
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
            ComMap(defaultSay);
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

/*        public ConnectedUser FounderUser
        {
            get
            {
                ConnectedUser connectedUser = null;
                server.ConnectedUsers.TryGetValue(FounderName, out connectedUser);
                return connectedUser;
            }
        }*/


        public async Task ProcessBattleSay(Say say)
        {
            ConnectedUser user;
            server.ConnectedUsers.TryGetValue(say.User, out user);
            if (say.Place == SayPlace.Battle && !say.IsEmote && user?.User.BanMute != true && user?.User.BanSpecChat != true) spring.SayGame(
                $"<{say.User}>{say.Text}"); // relay to spring
            
            // check if it's command
            if (!say.IsEmote && say.Text?.Length > 1 && say.Text.StartsWith("!"))
            {
                string[] allwords = say.Text.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (allwords.Length < 1) return;
                string com = allwords[0];

                // remove first word (command)
                string[] words = ZkData.Utils.ShiftArray(allwords, -1);

                string voteCom = "vote" + com;
                bool hasVoteVersion = !com.StartsWith("vote") && Commands.Commands.Any(x => x.Name == voteCom);

                if (!HasRights(com, say))
                {
                    if (hasVoteVersion)
                    {
                        com = voteCom;
                        if (!HasRights(voteCom, say)) return;
                    }
                    else return;
                }


                if (say.Place == SayPlace.User)
                {
                    if (com != "say" && com != "admins" && com != "help" && com != "helpall" && com != "springie" && com != "listoptions" &&
                        com != "spawn" && com != "predict" && com != "notify" && com != "transmit" && com != "adduser") SayBattle(
                            $"{com} executed by {say.User}");
                }

                await RunCommand(say, com, words);
            }
        }

        private void StartGame()
        {
            throw new NotImplementedException();
        }
    }
}