using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using PlasmaDownloader;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;
using ZkData;
using ZkData.UnitSyncLib;
using static System.String;

namespace ZkLobbyServer
{
    public partial class ServerBattle: Battle
    {
        public const int PollTimeout = 60;
        public static PlasmaDownloader.PlasmaDownloader downloader;
        public static SpringPaths springPaths;
        public static readonly Dictionary<string, BattleCommand> Commands = new Dictionary<string, BattleCommand>();


        private static object pickPortLock = new object();

        public readonly List<string> toNotify = new List<string>();

        private CommandPoll activePoll;
        public Resource HostedMap;

        public Resource HostedMod;

        public Mod HostedModInfo;

        private int hostingPort;
        private string hostingIp;

        private List<KickedPlayer> kickedPlayers = new List<KickedPlayer>();

        private Timer pollTimer;

        public ZkLobbyServer server;
        public Spring spring;

        static ServerBattle()
        {
            springPaths = new SpringPaths(GlobalConst.SpringieDataDir, false);
            downloader = new PlasmaDownloader.PlasmaDownloader(null, springPaths);
            downloader.PackageDownloader.SetMasterRefreshTimer(60);
            downloader.PackageDownloader.LoadMasterAndVersions(false);
            downloader.GetResource(DownloadType.ENGINE, GlobalConst.DefaultEngineOverride);

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
            PickHostingPort();
            hostingIp = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
        }

        private async Task ApplyBalanceResults(BalanceTeamsResult balance)
        {
            if (!string.IsNullOrEmpty(balance.Message)) await SayBattle(balance.Message);
            if (balance.Players != null && balance.Players.Count > 0)
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
            }

            if (balance.DeleteBots) foreach (var b in Bots.Keys) await server.Broadcast(Users.Keys, new RemoveBot() { Name = b });

            if (balance.Bots != null && balance.Bots.Count > 0)
            {
                foreach (var p in balance.Bots)
                {
                    Bots.AddOrUpdate(p.BotName,
                        s => new BotBattleStatus(s, p.Owner, p.BotAI),
                        (s, status) =>
                        {
                            status.owner = p.Owner;
                            status.aiLib = p.BotAI;
                            return status;
                        });

                }
            }
            foreach (var u in Users.Values.Select(x => x.ToUpdateBattleStatus()).ToList()) await server.Broadcast(Users.Keys, u); // send other's status to self
            foreach (var u in Bots.Values.Select(x => x.ToUpdateBotStatus()).ToList()) await server.Broadcast(Users.Keys, u);
        }


        public void Dispose()
        {
            Stop();
            spring.UnsubscribeEvents(this);
            springPaths.UnsubscribeEvents(this);
            pollTimer.Dispose();
            pollTimer = null;
        }

        public void FillDetails()
        {
            if (IsNullOrEmpty(Title)) Title = $"{FounderName}'s game";
            if (IsNullOrEmpty(EngineVersion)) EngineVersion = server.Engine;
            downloader.GetResource(DownloadType.ENGINE, server.Engine);

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
            if (spring.IsRunning) ret.AddRange(spring.Context.ActualPlayers.Select(x => x.Name));
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
                kickedPlayers.Add(new KickedPlayer() { Name = name });
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
                await RunCommandWithPermissionCheck(say, parts[0], parts.Skip(1).FirstOrDefault());
            }
        }

        public async Task ProcessPlayerJoin(UserBattleStatus ubs)
        {
            kickedPlayers.RemoveAll(x => x.TimeOfKicked <= DateTime.UtcNow.AddMinutes(-5));
            if (kickedPlayers.Any(y => y.Name == ubs.Name)) await KickFromBattle(ubs.Name, "Banned for five minutes");

            if (spring.IsRunning)
            {
                spring.AddUser(ubs.Name, ubs.ScriptPassword);
                var started = DateTime.UtcNow.Subtract(spring.IngameStartTime ?? DateTime.Now);
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
                await SayBattle("ServerManage error: " + ex);
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


        public void RunCommandDirectly<T>(Say e, string args = null) where T: BattleCommand, new()
        {
            var t = new T();
            t.Run(this, e, args);
        }


        public async Task RunCommandWithPermissionCheck(Say e, string com, string arg)
        {
            var cmd = GetCommandByName(com);
            if (cmd != null)
            {
                var perm = cmd.GetRunPermissions(this, e.User);
                if (perm == BattleCommand.RunPermission.Run) await cmd.Run(this, e, Join(" ", arg));
                else if (perm == BattleCommand.RunPermission.Vote) await StartVote(cmd, e, Join(" ", arg));
            }
        }


        public async Task<bool> RunServerBalance(bool isGameStart, int? allyTeams, bool? clanWise)
        {
            try
            {
                var context = GetContext();
                context.Mode = Mode;
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
            var context = GetContext();
            if (Mode != AutohostMode.None)
            {
                var balance = Balancer.BalanceTeams(context, true, null, null);
                if (!string.IsNullOrEmpty(balance.Message)) await SayBattle(balance.Message);
                if (!balance.CanStart) return;
                context.ApplyBalance(balance);
            }

            var startSetup = StartSetup.GetDedicatedServerStartSetup(context);
            
            spring.HostGame(startSetup, hostingIp, hostingPort, true);
            IsInGame = true;
            RunningSince = DateTime.UtcNow;
            foreach (var us in Users.Values)
            {
                if (us != null)
                {
                    ConnectedUser user;
                    if (server.ConnectedUsers.TryGetValue(us.Name, out user)) await user.SendCommand(GetConnectSpringStructure(us));
                }
            }
            await server.Broadcast(server.ConnectedUsers.Values, new BattleUpdate() { Header = GetHeader() });
        }

        public ConnectSpring GetConnectSpringStructure(UserBattleStatus us)
        {
            return new ConnectSpring()
            {
                Engine = EngineVersion,
                Ip = hostingIp,
                Port = hostingPort,
                Map = MapName,
                Game =  ModName,
                ScriptPassword = us.ScriptPassword
            };
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

        private void PickHostingPort()
        {
            var port = GlobalConst.UdpHostingPortStart;
            lock (pickPortLock)
            {
                var reservedPorts = server.Battles.Values.Where(x => x != null).Select(x => x.hostingPort).Distinct().ToDictionary(x => x, x => true);
                var usedPorts = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Where(x=>x!=null).Select(x => x.Port).Distinct().ToDictionary(x => x, x => true);

                while (usedPorts.ContainsKey(port) || reservedPorts.ContainsKey(port)) port++;
                hostingPort = port;
            }
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

            spring = new Spring(springPaths);

            spring.SpringExited += spring_SpringExited;

            spring.SpringStarted += spring_SpringStarted;
            spring.PlayerSaid += spring_PlayerSaid;
            spring.BattleStarted += spring_BattleStarted;
        }

        private void spring_BattleStarted(object sender, EventArgs e)
        {
            StopVote();
        }


        private void spring_PlayerSaid(object sender, SpringLogEventArgs e)
        {
            ProcessBattleSay(new Say() { User = e.Username, Text = e.Line, Place = SayPlace.Battle }); // process as command

            ConnectedUser user;
            if (server.ConnectedUsers.TryGetValue(e.Username, out user) && !user.User.BanMute) // relay
            {
                if (!e.Line.StartsWith("Allies:") && !e.Line.StartsWith("Spectators:"))
                {
                    server.GhostSay(new Say() { User = e.Username, Text = e.Line, Place = SayPlace.Battle }, BattleID);
                }
            }
        }


        private async void spring_SpringExited(object sender, Spring.SpringBattleContext springBattleContext)
        {
            StopVote();
            IsInGame = false;
            RunningSince = null;
            await server.Broadcast(server.ConnectedUsers.Keys, new BattleUpdate() { Header = GetHeader() });

            foreach (var s in toNotify)
            {
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
            }
            toNotify.Clear();

            await SayBattle(BattleResultHandler.SubmitSpringBattleResult(springBattleContext, server));
        }

        private void spring_SpringStarted(object sender, EventArgs e)
        {
            StopVote();
            if (HostedMod?.Mission != null)
            {
                var service = GlobalConst.GetContentService();
                foreach (var u in spring.LobbyStartContext.Players.Where(x => !x.IsSpectator)) service.NotifyMissionRun(u.Name, HostedMod.Mission.Name);
            }
        }

        public class KickedPlayer
        {
            public string Name;
            public DateTime TimeOfKicked = DateTime.UtcNow;
        }
    }
}