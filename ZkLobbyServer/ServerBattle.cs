using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;
using ZkData;
using ZkData.UnitSyncLib;
using static System.String;
using Timer = System.Timers.Timer;

namespace ZkLobbyServer
{
    public partial class ServerBattle: Battle
    {
        public const int PollTimeout = 60;
        public static PlasmaDownloader.PlasmaDownloader downloader;
        public static SpringPaths springPaths;
        public static readonly Dictionary<string, BattleCommand> Commands = new Dictionary<string, BattleCommand>();

        public readonly List<string> toNotify = new List<string>();

        private CommandPoll activePoll;
        public Resource HostedMap;

        public Resource HostedMod;

        public Mod HostedModInfo;

        private List<KickedPlayer> kickedPlayers = new List<KickedPlayer>();

        // login accepted - join channels

        private Timer pollTimer;

        public ZkLobbyServer server;
        public Spring spring;

        static ServerBattle()
        {
            springPaths = new SpringPaths(GlobalConst.SpringieDataDir, false);
            downloader = new PlasmaDownloader.PlasmaDownloader(null, springPaths);
            downloader.PackageDownloader.SetMasterRefreshTimer(60);
            downloader.PackageDownloader.LoadMasterAndVersions(false);
            downloader.GetEngine(GlobalConst.DefaultEngineOverride);

            Commands =
                Assembly.GetAssembly(typeof(BattleCommand))
                    .GetTypes()
                    .Where(x => !x.IsAbstract && x.IsClass && typeof(BattleCommand).IsAssignableFrom(x))
                    .Select(x => x.GetConstructor(new Type[] { }).Invoke(new object[] { }))
                    .Cast<BattleCommand>()
                    .ToDictionary(x => x.Shortcut, x => x);
        }

        public ServerBattle(ZkLobbyServer server)
        {
            this.server = server;

            pollTimer = new Timer(PollTimeout*1000);
            pollTimer.Enabled = false;
            pollTimer.AutoReset = false;
            pollTimer.Elapsed += pollTimer_Elapsed;
            SetupSpring();
        }

        public void ApplyBalanceResults(BalanceTeamsResult balance)
        {
            throw new NotImplementedException();
            /*
            if (!string.IsNullOrEmpty(balance.Message)) SayBattle(balance.Message, false);
            if (balance.Players != null && balance.Players.Count > 0)
            {

                foreach (var user in Users.Values.Where(x => !x.IsSpectator && !balance.Players.Any(y => y.Name == x.Name))) tas.ForceSpectator(user.Name); // spec those that werent in response
                foreach (var user in balance.Players.Where(x => x.IsSpectator)) tas.ForceSpectator(user.Name);

                bool comsharing = false;
                bool coopOptExists = tas.MyBattle.ModOptions.Any(x => x.Key.ToLower() == "coop");
                if (coopOptExists)
                {
                    KeyValuePair<string, string> comsharing_modoption = tas.MyBattle.ModOptions.FirstOrDefault(x => x.Key.ToLower() == "coop");
                    if (comsharing_modoption.Value != "0" && comsharing_modoption.Value != "false") comsharing = true;
                }
                foreach (var user in balance.Players.Where(x => !x.IsSpectator))
                {
                    tas.ForceTeam(user.Name, comsharing ? user.AllyID : user.TeamID);
                    tas.ForceAlly(user.Name, user.AllyID);
                }
            }

            if (balance.DeleteBots) foreach (var b in tas.MyBattle.Bots.Keys) tas.RemoveBot(b);
            if (balance.Bots != null && balance.Bots.Count > 0)
            {
                foreach (var b in tas.MyBattle.Bots.Values.Where(x => !balance.Bots.Any(y => y.BotName == x.Name && y.Owner == x.owner))) tas.RemoveBot(b.Name);

                foreach (var b in balance.Bots)
                {
                    var existing = tas.MyBattle.Bots.Values.FirstOrDefault(x => x.owner == b.Owner && x.Name == b.BotName);
                    if (existing != null)
                    {
                        tas.UpdateBot(existing.Name, b.BotAI, b.AllyID, b.TeamID);
                    }
                    else
                    {
                        tas.AddBot(b.BotName.Replace(" ", "_"), b.BotAI, b.AllyID, b.TeamID);
                    }
                }
            }*/
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

        public List<string> GetAllUserNames()
        {
            var ret = Users.Select(x => x.Key).ToList();
            if (spring.IsRunning) ret.AddRange(spring.StartContext.Players.Select(x => x.Name));
            ret.AddRange(spring.connectedPlayers.Keys);
            return ret.Distinct().ToList();
        }

        public BattleCommand GetCommandByName(string name)
        {
            BattleCommand command;
            if (Commands.TryGetValue(name, out command))
            {
                return command.Create();
            }
            return null;
        }


        public async Task KickFromBattle(string name, string reason)
        {
            UserBattleStatus user;
            if (Users.TryGetValue(name, out user))
            {
                kickedPlayers.Add(new KickedPlayer() {Name = name});
                var client = server.ConnectedUsers[name];
                await client.Respond($"You were kicked from battle by {name} : {reason}");
                await client.Process(new LeaveBattle() { BattleID = BattleID });
            }
        }

        public async Task ProcessBattleSay(Say say)
        {
            if (say.User == GlobalConst.NightwatchName) return; // ignore self

            ConnectedUser user;
            server.ConnectedUsers.TryGetValue(say.User, out user);
            if (say.Place == SayPlace.Battle && !say.IsEmote && user?.User.BanMute != true && user?.User.BanSpecChat != true) spring.SayGame($"<{say.User}>{say.Text}"); // relay to spring

            // check if it's command
            if (!say.IsEmote && say.Text?.Length > 1 && say.Text.StartsWith("!"))
            {
                var parts = say.Text.Substring(1).Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                await RunCommand(say, parts[0], parts.Skip(1).FirstOrDefault());
            }
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


        public void RunCommand<T>(Say e, string args = null) where T: BattleCommand, new()
        {
            var t = new T();
            t.Run(this, e, args);
        }


        public async Task RunCommand(Say e, string com, string arg)
        {
            var cmd = GetCommandByName(com);
            if (cmd != null)
            {
                var perm = cmd.GetRunPermissions(this, e.User);
                if (perm == BattleCommand.RunPermission.Run) await cmd.Run(this, e, Join(" ", arg));
                else if (perm == BattleCommand.RunPermission.Vote) await StartVote(cmd, e, Join(" ", arg));
            }
        }


        public bool RunServerBalance(bool isGameStart, int? allyTeams, bool? clanWise)
        {
            try
            {
                var context = GetContext();
                context.mode = Mode;
                var balance = Balancer.BalanceTeams(context, isGameStart, allyTeams, clanWise);
                ApplyBalanceResults(balance);
                return balance.CanStart;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return false;
            }
        }


        public async Task SayBattle(string text, string privateUser = null)
        {
            if (!IsNullOrEmpty(text))
                foreach (var line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (privateUser == null && spring?.IsRunning == true) spring.SayGame(line);
                    await
                        server.GhostSay(
                            new Say()
                            {
                                User = GlobalConst.NightwatchName,
                                Text = line,
                                Place = privateUser != null ? SayPlace.BattlePrivate : SayPlace.Battle,
                                Target = privateUser,
                                IsEmote = true
                            },
                            BattleID);
                }
        }

        public async Task SetModOptions(Dictionary<string, string> options)
        {
            ModOptions = options;
            await server.Broadcast(Users.Keys, options);
        }


        public async Task Spectate(string name)
        {
            ConnectedUser usr;
            if (server.ConnectedUsers.TryGetValue(name, out usr)) await usr.Process(new UpdateUserBattleStatus() { Name = usr.Name, IsSpectator = true });
        }

        public async Task StartGame()
        {
            //spring.lobbyUserName = tas.UserName; // hack until removed when springie moves to server
            //spring.lobbyPassword = tas.UserPassword;  // spring class needs this to submit results
            var ip = "127.0.0.1";
            int port = 8452;

            var startContext = GetContext();
            var startSetup = StartSetup.GetSpringBattleStartSetup(startContext);
            if (startSetup.BalanceTeamsResult != null)
            {
                startContext.Players = startSetup.BalanceTeamsResult.Players;
                startContext.Bots = startSetup.BalanceTeamsResult.Bots;
            }

            spring.HostGame(startContext, ip, port, null, null, EngineVersion, startSetup);  // TODO HACK GET PORTS
            RunningSince = DateTime.UtcNow;
            foreach (var us in Users.Values)
            {
                if (us != null)
                {
                    await
                        server.Broadcast(Users.Keys,
                            new ConnectSpring()
                            {
                                Engine = EngineVersion,
                                Ip = ip,
                                Port = port,
                                Resources = new List<string>() { MapName, ModName },
                                ScriptPassword = us.ScriptPassword
                            });
                }
            }
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = GetHeader() });

        }


        public async Task StartVote(BattleCommand command, Say e, string args)
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
                pollTimer.Interval = PollTimeout*1000;
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

        public async Task SwitchEngine(string engine)
        {
            EngineVersion = engine;
            FillDetails();
            await
                server.Broadcast(server.ConnectedUsers.Values,
                    new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Engine = EngineVersion } });
        }

        public async Task SwitchGame(string internalName)
        {
            ModName = internalName;
            FillDetails();
            await
                server.Broadcast(server.ConnectedUsers.Values,
                    new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Game = ModName } });
        }

        public async Task SwitchGameType(AutohostMode type)
        {
            Mode = type;
            FillDetails();
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = GetHeader() });
            // do a full update - mode can also change map/players
        }

        public async Task SwitchMap(string internalName)
        {
            MapName = internalName;
            FillDetails();
            await
                server.Broadcast(server.ConnectedUsers.Values,
                    new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Map = MapName } });
        }

        public async Task SwitchMaxPlayers(int cnt)
        {
            MaxPlayers = cnt;
            FillDetails();
            await
                server.Broadcast(server.ConnectedUsers.Values,
                    new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, MaxPlayers = MaxPlayers } });
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
            FillDetails();
            await
                server.Broadcast(server.ConnectedUsers.Values,
                    new BattleUpdate() { Header = new BattleHeader() { BattleID = BattleID, Title = Title } });
        }

        public override void UpdateWith(BattleHeader h)
        {
            base.UpdateWith(h);
            RunningSince = null; // todo hook to spring
            FillDetails();
        }


        private void pollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                pollTimer.Stop();
                if (activePoll != null) activePoll.End();
                StopVote();
            }
            catch {}
            finally
            {
                pollTimer.Start();
            }
        }


        private void SetupSpring()
        {
            spring?.UnsubscribeEvents(this);

            spring = new Spring(springPaths) { UseDedicatedServer = true };

            spring.SpringExited += spring_SpringExited;
            spring.GameOver += spring_GameOver;

            spring.SpringStarted += spring_SpringStarted;
            spring.PlayerSaid += spring_PlayerSaid;
            spring.BattleStarted += spring_BattleStarted;
        }

        private void spring_BattleStarted(object sender, EventArgs e)
        {
            StopVote();
        }

        private void spring_GameOver(object sender, SpringLogEventArgs e)
        {
            SayBattle("Game over, exiting");
            // Spring sends GAMEOVER for every player and spec, we only need the first one.
            spring.GameOver -= spring_GameOver;
            Utils.SafeThread(() =>
            {
                // Wait for gadgets that send spring autohost messages after gadget:GameOver()
                // such as awards.lua
                Thread.Sleep(10000);
                spring.ExitGame();
                spring.GameOver += spring_GameOver;
            }).Start();
        }


        private void spring_PlayerSaid(object sender, SpringLogEventArgs e)
        {
            /*tas.GameSaid(e.Username, e.Line);
            User us;
            tas.ExistingUsers.TryGetValue(e.Username, out us);
            bool isMuted = us != null && us.BanMute;
            if (Program.main.Config.RedirectGameChat && e.Username != tas.UserName && !e.Line.StartsWith("Allies:") &&
                !e.Line.StartsWith("Spectators:") && !isMuted) tas.Say(SayPlace.Battle, "", "[" + e.Username + "]" + e.Line, false);*/
        }


        private async void spring_SpringExited(object sender, EventArgs e)
        {
            StopVote();
            IsInGame = false;
            RunningSince = null;
            await server.Broadcast(server.ConnectedUsers.Keys, new BattleUpdate() { Header = GetHeader() });
            
            foreach (string s in toNotify)
            {
                await server.GhostSay(new Say()
                {
                    User = GlobalConst.NightwatchName,
                    Text = $"** {FounderName} 's {Title} just ended, join me! **",
                    Target = s,
                    IsEmote = true,
                    Place = SayPlace.User,
                    Ring = true,
                    AllowRelay = false
                });
            }
            toNotify.Clear();
        }

        private void spring_SpringStarted(object sender, EventArgs e)
        {
            StopVote();
            if (HostedMod?.Mission != null)
            {
                var service = GlobalConst.GetContentService();
                foreach (var u in spring.StartContext.Players.Where(x => !x.IsSpectator)) service.NotifyMissionRun(u.Name, HostedMod.Mission.Name);
            }
        }


        private void tas_BattleUserJoined(object sender, BattleUserEventArgs e1)
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

                */
        }

        private void tas_BattleUserLeft(object sender, BattleUserEventArgs e1)
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

        private void tas_MyBattleMapChanged(object sender, OldNewPair<Battle> oldNewPair)
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



        public class KickedPlayer
        {
            public string Name;
            public DateTime TimeOfKicked = DateTime.UtcNow;
        }
    }
}