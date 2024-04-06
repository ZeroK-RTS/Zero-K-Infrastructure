using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData;
using Timer = System.Timers.Timer;

namespace LobbyClient
{
    /// <summary>
    ///     represents one install location of spring game
    /// </summary>
    public class DedicatedServer : IDisposable
    {
        public static EventHandler AnyDedicatedStarted;
        public static EventHandler<SpringBattleContext> AnyDedicatedExited;

        private readonly SpringPaths paths;
        private readonly Timer timer = new Timer(20000);

        private Dictionary<string, HashSet<byte> > gamePrivateMessages = new Dictionary<string, HashSet<byte> >();

        private Process process;
        private string scriptPath;
        private Talker talker;

        public SpringBattleContext Context { get; private set; }

        public DateTime? IngameStartTime => Context?.IngameStartTime;


        public bool IsRunning
        {
            get
            {
                try
                {
                    return (process != null) && !process.HasExited;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error determining process state: {0}", ex);
                    return false;
                }
            }
        }

        public LobbyHostingContext LobbyStartContext => Context.LobbyStartContext;

        public DedicatedServer(SpringPaths springPaths)
        {
            paths = springPaths;
            timer.Elapsed += timer_Elapsed;
        }

        public void Dispose()
        {
            talker?.UnsubscribeEvents(this);
            talker?.Dispose();
            timer?.Dispose();
            process?.UnsubscribeEvents(this);
            Context = null;
            scriptPath = null;
            gamePrivateMessages = null;
            process = null;
            talker = null;
        }

        public class MidGameJoinUser
        {
            public string Name { get; set; }
            public string Avatar { get; set; }
            public string Icon { get; set; }
            public string Badges { get; set; }
            public bool IsAdmin { get; set; }
            public string Clan { get; set; }
            public string Faction { get; set; }
            public string Country { get; set; }

            public MidGameJoinUser(User user)
            {
                Name = user.Name;
                Avatar = user.Avatar;
                Icon = user.Icon;
                if (user.Badges != null) Badges = string.Join(",", user.Badges);
                IsAdmin = user.IsAdmin;
                Clan = user.Clan;
                Faction = user.Faction;
                Country = user.Country;
            }
            
            public override string ToString()
            {
                return string.Join("|", new string[]{Name, Avatar, Icon, Badges, IsAdmin.ToString(), Clan, Faction, Country});
            }
        }

        /// <summary>
        ///     Adds user dynamically to running game - for security reasons add his script
        /// </summary>
        public void AddUser(string name, string scriptPassword, User user)
        {
            if (IsRunning)
            {
                talker.SendText($"/adduser {name} {scriptPassword}");
                if (user != null) talker.SendText($"SPRINGIE:User {new MidGameJoinUser(user)}");
            }
            
        }

        public event EventHandler<SpringBattleContext> BattleStarted = (sender, args) => { };

        //public event EventHandler<> 
        /// <summary>
        ///     Data is true if exit was crash
        /// </summary>
        public event EventHandler<SpringBattleContext> DedicatedServerExited;
        public event EventHandler DedicatedServerStarted;


        public void ExitGame()
        {
            try
            {
                if (IsRunning)
                {
                    SayGame("/kill");
                    process.WaitForExit(20000);
                    if (!IsRunning) return;

                    Trace.TraceWarning("Terminating dedicated server process due to /kill timeout");
                    Context.WasKilled = true;
                    process.Kill();

                    process.WaitForExit(1000);
                    if (!IsRunning) return;
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error quitting dedicated server: {0}", ex);
            }
        }


        public void ForceStart()
        {
            if (IsRunning)
            {
                talker.SendText("/forcestart");
                Context.IsForceStarted = true;
            }
        }

        public event EventHandler<SpringLogEventArgs> GameOver; // game has ended


        public string HostGame(LobbyHostingContext startContext, string host, int port)
        {
            if (!File.Exists(paths.GetDedicatedServerPath(startContext.EngineVersion))) throw new ApplicationException($"Dedicated server executable not found: {paths.GetDedicatedServerPath(startContext.EngineVersion)}");

            Context = new SpringBattleContext();
            Context.SetForHosting(startContext, host, port, null, null);

            if (!IsRunning)
            {
                talker = new Talker();
                talker.SpringEvent += talker_SpringEvent;
                Context.IsHosting = true;

                scriptPath = Utils.MakePath(paths.WritableDirectory, "script_" + startContext.FounderName + ".txt").Replace('\\', '/');

                var script = ScriptGenerator.GenerateHostScript(Context, talker.LoopbackPort);
                timer.Start();

                StartDedicated(script);
                return script;
            }
            else Trace.TraceError("Dedicated server already running");
            return null;
        }


        public void Kick(string name)
        {
            SayGame("/kick " + name);

            /* Kick doesn't prevent rejoin, so also resign the target
             * so they can't just come back and control stuff again */
            SayGame("/luarules resignteam " + name);
        }

        public event EventHandler<SpringLogEventArgs> PlayerDisconnected;
        public event EventHandler<SpringLogEventArgs> PlayerJoined;
        public event EventHandler<SpringLogEventArgs> PlayerLeft;
        public event EventHandler<SpringLogEventArgs> PlayerLost; // player lost the game
        public event EventHandler<SpringChatEventArgs> PlayerSaid;

        public void ResignPlayer(string name)
        {
            if (IsRunning) talker.SendText($"/luarules resignteam {name}");
        }

        public void RunLocalScriptGame(string script, string engine)
        {
            Context = new SpringBattleContext();
            Context.SetForSelfHosting(engine);
            StartDedicated(script);
        }

        public void SayGame(string text)
        {
            try
            {
                if ((text == "/cheat") || (text?.StartsWith("/cheat ") == true)) Context.IsCheating = true;
                talker.SendText(text);
            }
            catch (NullReferenceException)
            {
                // do nothing: null reference is expected when game is not running
                // the property isRunning would be useful here if it didn't lie so much
            }
        }

        public void WaitForExit()
        {
            process.WaitForExit();
        }

        private void AddToLogs(Talker.SpringEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(e.Text) || string.IsNullOrEmpty(e.PlayerName)) return;

                var s = "CHATLOG:" + e.PlayerName + " ";
                switch (e.Param)
                {
                    case Talker.TO_EVERYONE:
                        s = s + "<PUBLIC> ";
                        break;
                    case Talker.TO_ALLIES:
                        s = s + "<ALLY> ";
                        break;
                    case Talker.TO_SPECTATORS:
                        s = s + "<SPEC> ";
                        break;
                    default:
                        s = s + "<PRIV> ";
                        break;
                }
                s = s + e.Text;
                Context.OutputExtras.Add(s);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while processing '{0}' :{1}", e.Text, ex);
            }
        }


        private void dedicatedProcess_Exited(object sender, EventArgs e)
        {
            try
            {
                Context.IsCrash = (process.ExitCode != 0) && !Context.WasKilled;
                process.UnsubscribeEvents(this);
                try
                {
                    if (!process.WaitForExit(2000)) process.Kill();
                }
                catch { }

                process = null;
                talker.UnsubscribeEvents(this);
                talker?.Close();
                talker = null;
                Thread.Sleep(1000);

                if (LobbyStartContext != null)
                    foreach (var p in Context.ActualPlayers)
                        p.IsIngame = false;

                if (File.Exists(scriptPath))
                {
                    try
                    {
                        File.Delete(scriptPath);
                    }
                    catch { }
                }

                if (Context.OutputExtras.Count(x => x.StartsWith("award")) == 0)
                {
                    //No awards received, do a strict majority vote on awards
                    int playersReportingAwards = gamePrivateMessages.Where(x => x.Key.StartsWith("award")).SelectMany(x => x.Value).Distinct().Count();
                    Context.OutputExtras = gamePrivateMessages.Where(x => x.Value.Count >= playersReportingAwards / 2 + 1).Select(x => x.Key).ToList();
                }

                DedicatedServerExited?.Invoke(this, Context);
                AnyDedicatedExited?.Invoke(this, Context);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing dedicated server exit: {0}", ex);
            }
        }


        private void HandleSpecialMessages(Talker.SpringEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(e.Text) || !e.Text.StartsWith("SPRINGIE:")) return;


                var text = e.Text.Substring(9);


                if (!gamePrivateMessages.ContainsKey(text))
                {
                    gamePrivateMessages.Add(text, new HashSet<byte>());
                }
                gamePrivateMessages[text].Add(e.PlayerNumber);

                if (text.StartsWith("READY:"))
                {
                    var name = text.Substring(6);
                    var entry = Context.ActualPlayers.FirstOrDefault(x => x.Name == name);
                    if (entry != null) entry.IsIngameReady = true;
                }

                if (gamePrivateMessages[text].Count() != Context.LobbyStartContext.Players.Count() / 2 + 1) return; // only accept messages if count matches N/2+1 exactly

                if (text == "FORCE") ForceStart();

                Context.OutputExtras.Add(text);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while processing '{0}' :{1}", e.Text, ex);
            }
        }

        private void MarkPlayerDead(string name, bool isDead)
        {
            var sp = Context.ActualPlayers.FirstOrDefault(x => x.Name == name);
            if (sp != null)
            {
                sp.LoseTime = isDead ? (int)DateTime.UtcNow.Subtract(Context.IngameStartTime ?? Context.StartTime).TotalSeconds : (int?)null;
                if (sp.QuitTime < sp.LoseTime) sp.LoseTime = sp.QuitTime;
            }
        }

        private void StartDedicated(string script)
        {
            scriptPath = Path.GetTempFileName();
            File.WriteAllText(scriptPath, script);

            process = new Process { StartInfo = { CreateNoWindow = true } };

            paths.SetDefaultEnvVars(null, Context.EngineVersion);

            var arg = new List<string>();

            process.StartInfo.FileName = paths.GetDedicatedServerPath(Context.EngineVersion);
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.GetDedicatedServerPath(Context.EngineVersion));

            arg.Add($"\"{scriptPath}\"");

            Context.StartTime = DateTime.UtcNow;
            gamePrivateMessages = new Dictionary<string, HashSet<byte>>();
            process.StartInfo.Arguments = string.Join(" ", arg);
            process.Exited += dedicatedProcess_Exited;

            // use shell execute, this prevents handle inheritance and allows 8200 port to be reused if server crashes
            // alternative: http://stackoverflow.com/questions/3342941/kill-child-process-when-parent-process-is-killed
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.RedirectStandardError = false;
            process.EnableRaisingEvents = true;

            process.Start();

            
            if (IsRunning)
            {
                DedicatedServerStarted?.Invoke(this, EventArgs.Empty);
                AnyDedicatedStarted?.Invoke(this, EventArgs.Empty);
            }
        }


        private void talker_SpringEvent(object sender, Talker.SpringEventArgs e)
        {
            try
            {
                switch (e.EventType)
                {
                    case Talker.SpringEventType.PLAYER_JOINED:
                        var entry = Context?.GetOrAddPlayer(e.PlayerName);
                        if (entry != null)
                        {
                            entry.IsIngame = true;
                            entry.QuitTime = null;
                        }
                        PlayerJoined?.Invoke(this, new SpringLogEventArgs(e.PlayerName));
                        break;

                    case Talker.SpringEventType.PLAYER_LEFT:
                        entry = Context?.GetOrAddPlayer(e.PlayerName);
                        if (entry != null)
                        {
                            entry.IsIngame = false;
                            entry.QuitTime = (int)DateTime.UtcNow.Subtract(Context.IngameStartTime ?? Context.StartTime).TotalSeconds;
                        }
                        if (e.Param == 0) PlayerDisconnected?.Invoke(this, new SpringLogEventArgs(e.PlayerName));
                        PlayerLeft?.Invoke(this, new SpringLogEventArgs(e.PlayerName));
                        break;

                    case Talker.SpringEventType.GAME_LUAMSG:
                        HandleSpecialMessages(e);
                        break;

                    case Talker.SpringEventType.PLAYER_CHAT:
                        if (e.Param == 255) HandleSpecialMessages(e);
                        else AddToLogs(e);
                        
                        if ((PlayerSaid != null) && !string.IsNullOrEmpty(e.PlayerName))
                        {
                            SpringChatLocation location = SpringChatLocation.Private;
                            if (((e.Param == Talker.TO_EVERYONE) || (e.Param == Talker.TO_EVERYONE_LEGACY))) location = SpringChatLocation.Public;
                            if (e.Param == Talker.TO_ALLIES) location = SpringChatLocation.Allies;
                            if (e.Param == Talker.TO_SPECTATORS) location = SpringChatLocation.Spectators;
                            PlayerSaid(this, new SpringChatEventArgs(e.PlayerName, e.Text, location));
                        }
                        break;

                    case Talker.SpringEventType.PLAYER_DEFEATED:
                        MarkPlayerDead(e.PlayerName, true);
                        if (PlayerLost != null) PlayerLost(this, new SpringLogEventArgs(e.PlayerName));
                        break;

                    case Talker.SpringEventType.SERVER_GAMEOVER:
                        if (!Context.GameEndedOk) // server gameover runs multiple times
                        {
                            foreach (var p in Context.ActualPlayers)
                            {
                                if (!p.IsIngame && !p.IsSpectator) MarkPlayerDead(p.Name, true);
                                p.IsIngame = false;
                            }

                            // set victory team for all allied with currently alive
                            if (e.winningAllyTeams.Length > 0) {
                                foreach (var ally in e.winningAllyTeams) foreach (var p in Context.ActualPlayers.Where(x => !x.IsSpectator && (x.AllyNumber == ally))) p.IsVictoryTeam = true;
                            } else { // Fallback, shouldn't happen
                                foreach (var p in Context.ActualPlayers.Where(x => !x.IsSpectator && (x.LoseTime == null))) foreach (var q in Context.ActualPlayers.Where(x => !x.IsSpectator && (x.AllyNumber == p.AllyNumber))) q.IsVictoryTeam = true;
                            }

                            if (Context.IngameStartTime != null)
                            {
                                Context.GameEndedOk = true;
                                Context.Duration = (int)DateTime.UtcNow.Subtract(Context.IngameStartTime ?? Context.StartTime).TotalSeconds;

                                GameOver?.Invoke(this, new SpringLogEventArgs(e.PlayerName));
                            }
                            else Trace.TraceWarning("recieved GAMEOVER before STARTPLAYING!");

                            Task.Delay(10000).ContinueWith(x => ExitGame());
                        }
                        break;

                    case Talker.SpringEventType.PLAYER_READY:
                        if (e.Param == 1)
                        {
                            entry = Context.GetOrAddPlayer(e.PlayerName);
                            if (entry != null) entry.IsIngameReady = true;
                        }
                        break;

                    case Talker.SpringEventType.SERVER_STARTPLAYING:
                        Context.ReplayName = e.ReplayFileName;
                        Context.EngineBattleID = e.GameID;
                        Context.IngameStartTime = DateTime.UtcNow;
                        Context.PlayersUnreadyOnStart = Context.ActualPlayers.Where(x => !x.IsSpectator && !(x.IsIngameReady && x.IsIngame)).Select(x => x.Name).ToList();
                        foreach (var p in Context.ActualPlayers.Where(x => !x.IsSpectator)) p.IsIngameReady = true;

                        process.PriorityClass = ProcessPriorityClass.High;

                        BattleStarted(this, Context);
                        break;

                    case Talker.SpringEventType.SERVER_QUIT:
                        if (LobbyStartContext != null) foreach (var p in Context.ActualPlayers) p.IsIngame = false;
                        //if (GameOver != null) GameOver(this, new SpringLogEventArgs(e.PlayerName));
                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing spring message:{0}", ex);
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var timeSinceStart = DateTime.UtcNow.Subtract(Context.StartTime).TotalSeconds;
                const int timeToWait = 160; // force start after 180s
                const int timeToWarn = 100; // warn people after 120s 

                if (Context.IsHosting && IsRunning && (Context.IngameStartTime == null))
                {
                    if (timeSinceStart > timeToWait)
                    {
                        Context.IsTimeoutForceStarted = true;
                        ForceStart();
                    }
                    else if (timeSinceStart > timeToWarn) SayGame($"Game will be force started in {Math.Max(20, timeToWait - Math.Round(timeSinceStart))} seconds");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error checking start: {0}", ex);
            }
        }
    }

}
