using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
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
using ZkLobbyServer.autohost;
using static System.String;
using Timer = System.Timers.Timer;
using Utils = Springie.Utils;

namespace ZkLobbyServer
{
    public partial class ServerBattle:Battle
    {
        public static PlasmaDownloader.PlasmaDownloader downloader;
        public static SpringPaths springPaths;

        public const int PollTimeout = 60;
        public static readonly Dictionary<string, ServerBattleCommand> Commands = new Dictionary<string, ServerBattleCommand>();

        public ZkLobbyServer server;

        CommandPoll activePoll;
        Timer pollTimer;

        public Resource HostedMod;
        public Resource HostedMap;
        public Spring spring;

        internal static Say defaultSay = new Say() { Place = SayPlace.Battle, User = "NightWatch"};

        static ServerBattle()
        {
            springPaths = new SpringPaths(GlobalConst.SpringieDataDir, false);
            downloader = new PlasmaDownloader.PlasmaDownloader(null, springPaths);
            downloader.PackageDownloader.SetMasterRefreshTimer(60);
            downloader.PackageDownloader.LoadMasterAndVersions(false);
            downloader.GetEngine(GlobalConst.DefaultEngineOverride);

            Commands =
                Assembly.GetAssembly(typeof(ServerBattleCommand))
                    .GetTypes()
                    .Where(x => !x.IsAbstract && x.IsClass && typeof(ServerBattleCommand).IsAssignableFrom(x))
                    .Select(x => x.GetConstructor(new Type[] { }).Invoke(new object[] { }))
                    .Cast<ServerBattleCommand>()
                    .ToDictionary(x => x.Shortcut, x => x);

        }

        public ServerBattle(ZkLobbyServer server)
        {
            this.server = server;
           
            pollTimer = new Timer(PollTimeout * 1000);
            pollTimer.Enabled = false;
            pollTimer.AutoReset = false;
            pollTimer.Elapsed += pollTimer_Elapsed;


            lastMapChange = DateTime.Now;

            SetupSpring();
        }

        public Mod HostedModInfo;

        public void FillDetails()
        {
            if (IsNullOrEmpty(Title)) Title = $"{FounderName}'s " + Mode.Description();
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


            HostedMod = MapPicker.FindResources(ResourceType.Mod, ModName ?? "zk:stable").FirstOrDefault();
            HostedMap = MapName != null
                ? MapPicker.FindResources(ResourceType.Map, MapName).FirstOrDefault()
                : MapPicker.GetRecommendedMap(GetContext());

            ModName = HostedMod?.InternalName ?? ModName ?? "zk:stable";
            MapName = HostedMap?.InternalName ?? MapName ?? "Small_Divide-Remake-v04";


            if (HostedMod != null)
            {
                try
                {
                    HostedModInfo = MetaDataCache.ServerGetMod(HostedMod.InternalName);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Error loading mod metadata for {0} : {1}", HostedMod.InternalName, ex);
                }
            }
        }


        public void RunCommand<T>(Say e,string args = null) where T:ServerBattleCommand, new()
        {
            var t = new T();
            t.Run(this, e, args);
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

        

        public async Task RegisterVote(Say e, bool vote)
        {
            if (activePoll != null)
            {
                if (await activePoll.Vote(e, vote))
                {
                    pollTimer.Enabled = false;
                    activePoll = null;
                }
            }
            else await Respond(e, "There is no poll going on, start some first");
        }


        public Task Respond(Say e, string text)
        {
            return SayBattle(text, e?.User);
        }

        public ServerBattleCommand GetCommandByName(string name)
        {
            ServerBattleCommand command;
            if (Commands.TryGetValue(name, out command))
            {
                return command.Create();
            }
            return null;
        }

        public async Task SwitchMap(string internalName)
        {
            MapName = internalName;
            FillDetails();
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Map = MapName } });
        }

        public async Task SwitchGame(string internalName)
        {
            ModName = internalName;
            FillDetails();
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Game = ModName } });
        }


        public async Task RunCommand(Say e, string com, string arg)
        {
            var cmd = GetCommandByName(com);
            if (cmd != null)
            {
                var perm = cmd.RunPermissions(this, e.User);
                if (perm == CommandExecutionRight.Run) await cmd.Run(this, e, string.Join(" ", arg));
                else if (perm == CommandExecutionRight.Vote) await StartVote(cmd, e, string.Join(" ", arg));
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


        public async Task StartVote(ServerBattleCommand command, Say e, string args)
        {
                if (activePoll != null)
                {
                    await Respond(e, "Another poll already in progress, please wait");
                    return;
                }
                var poll = new CommandPoll(this);
                if (await poll.Setup(command, e, args))
                {
                    activePoll = poll;
                    pollTimer.Interval = PollTimeout * 1000;
                    pollTimer.Enabled = true;
                }
        }


        public void Stop()
        {
            StopVote();
        }

        public async void StopVote(Say e = null)
        {
            if (activePoll != null) await activePoll.End();
            if (pollTimer != null) pollTimer.Enabled = false;
            activePoll = null;
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
        }

        void spring_SpringStarted(object sender, EventArgs e)
        {
            //lockedUntil = DateTime.MinValue;
            //tas.ChangeLock(false);
            if (HostedMod.Mission != null)
            {
                var service = GlobalConst.GetContentService();
                foreach (UserBattleStatus u in Users.Values.Where(x => !x.IsSpectator)) service.NotifyMissionRun(u.Name, HostedMod.Mission.Name);
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
            if (say.User == GlobalConst.NightwatchName) return; // ignore self

            ConnectedUser user;
            server.ConnectedUsers.TryGetValue(say.User, out user);
            if (say.Place == SayPlace.Battle && !say.IsEmote && user?.User.BanMute != true && user?.User.BanSpecChat != true) spring.SayGame(
                $"<{say.User}>{say.Text}"); // relay to spring
            
            // check if it's command
            if (!say.IsEmote && say.Text?.Length > 1 && say.Text.StartsWith("!"))
            {
                var parts = say.Text.Substring(1).Split(new[] { ' ' }, 2 , StringSplitOptions.RemoveEmptyEntries);
                await RunCommand(say, parts[0], parts[1]);
            }
        }

        private void StartGame()
        {
            throw new NotImplementedException();
        }

        public async Task SwitchEngine(string engine)
        {
            EngineVersion = engine;
            FillDetails();
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Engine = EngineVersion } });
        }

        public async Task SwitchTitle(string title)
        {
            Title = title;
            FillDetails();
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Title = Title } });
        }

        public async Task SwitchMaxPlayers(int cnt)
        {
            MaxPlayers = cnt;
            FillDetails();
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, MaxPlayers = MaxPlayers } });
        }

        public async Task SwitchGameType(AutohostMode type)
        {
            Mode = type;
            FillDetails();
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = GetHeader() }); // do a full update - mode can also change map/players
        }

        public async Task SwitchPassword(string pwd)
        {
            Password = pwd;
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = GetHeader() }); // do a full update to hide pwd properly
        }

        public async Task KickFromBattle(string name, string reason)
        {
            UserBattleStatus user;
            if (Users.TryGetValue(name, out user))
            {
                var client = server.ConnectedUsers[name];
                await client.Respond($"You were kicked from battle by {name} : {reason}");
                await client.Process(new LeaveBattle() { BattleID = BattleID });
            }
        }

        public class KickedPlayer
        {
            public string Name;
            public DateTime TimeOfKicked = DateTime.UtcNow;
        }

        public List<KickedPlayer> kickedPlayers = new List<KickedPlayer>();


        public async Task Spectate(string name)
        {
            // TODO rebalance
            ConnectedUser usr;
            if (server.ConnectedUsers.TryGetValue(name, out usr)) await usr.Process(new UpdateUserBattleStatus() { Name = usr.Name, IsSpectator = true });
        }
    }
}