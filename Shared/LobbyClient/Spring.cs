#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using PlasmaShared;
using ZkData;
using Timer = System.Timers.Timer;

#endregion

namespace LobbyClient
{
    /// <summary>
    ///     represents one install location of spring game
    /// </summary>
    public class Spring: IDisposable
    {
        public delegate void LogLine(string text, bool isError);

        public static EventHandler AnySpringStarted;
        public static EventHandler<EventArgs<bool>> AnySpringExited;

        private readonly SpringPaths paths;
        private readonly Timer timer = new Timer(20000);

        private Dictionary<string, int> gamePrivateMessages = new Dictionary<string, int>();

        private Process process;
        private string scriptPath;
        private Talker talker;

        public SpringBattleContext Context { get; private set; }

        public DateTime GameExited { get; private set; }

        public DateTime? IngameStartTime => Context?.IngameStartTime;


        public bool IsRunning
        {
            get
            {
                try
                {
                    return process != null && !process.HasExited;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error determining process state: {0}", ex);
                    return false;
                }
            }
        }

        public LobbyHostingContext LobbyStartContext => Context.LobbyStartContext;

        public Spring(SpringPaths springPaths)
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

        /// <summary>
        ///     Adds user dynamically to running game - for security reasons add his script
        /// </summary>
        public void AddUser(string name, string scriptPassword)
        {
            if (IsRunning) talker.SendText($"/adduser {name} {scriptPassword}");
        }

        public event EventHandler BattleStarted = (sender, args) => { };


        public string ConnectGame(string ip, int port, string myName, string myPassword, string engine)
        {
            Context = new SpringBattleContext();
            Context.SetForConnecting(ip, port, myName, myPassword, engine);
            var script = ScriptGenerator.GenerateConnectScript(Context);
            StartSpring(script);
            return script;
        }

        public void ExitGame()
        {
            try
            {
                if (IsRunning)
                {
                    SayGame("/kill"); // todo dont do this if talker does not work (not a host)
                    process.WaitForExit(20000);
                    if (!IsRunning) return;

                    Console.WriteLine("Terminating Spring process due to /kill timeout");
                    Context.WasKilled = true;
                    process.Kill();

                    process.WaitForExit(1000);
                    if (!IsRunning) return;
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error quitting spring: {0}", ex);
            }
        }


        public void ForceStart()
        {
            if (IsRunning) talker.SendText("/forcestart");
        }

        public event EventHandler<SpringLogEventArgs> GameOver; // game has ended


        public string HostGame(LobbyHostingContext startContext,
            string host,
            int port,
            bool useDedicated,
            string myName = null,
            string myPassword = null)
        {
            if (!File.Exists(paths.GetSpringExecutablePath(startContext.EngineVersion)) &&
                !File.Exists(paths.GetDedicatedServerPath(startContext.EngineVersion)))
                throw new ApplicationException(
                    $"Spring or dedicated server executable not found: {paths.GetSpringExecutablePath(startContext.EngineVersion)}, {paths.GetDedicatedServerPath(startContext.EngineVersion)}");

            Context = new SpringBattleContext();
            Context.SetForHosting(startContext, host, port, useDedicated, myName, myPassword);

            if (!IsRunning)
            {
                talker = new Talker();
                talker.SpringEvent += talker_SpringEvent;
                Context.IsHosting = true;

                scriptPath = Utils.MakePath(paths.WritableDirectory, "script_" + myName + ".txt").Replace('\\', '/');

                var script = ScriptGenerator.GenerateHostScript(Context, talker.LoopbackPort);
                timer.Start();

                StartSpring(script);
                return script;
            }
            else Trace.TraceError("Spring already running");
            return null;
        }


        public void Kick(string name)
        {
            SayGame("/kick " + name);
        }

        public event LogLine LogLineAdded = delegate { };
        public event EventHandler<SpringLogEventArgs> PlayerDisconnected;
        public event EventHandler<SpringLogEventArgs> PlayerJoined;
        public event EventHandler<SpringLogEventArgs> PlayerLeft;
        public event EventHandler<SpringLogEventArgs> PlayerLost; // player lost the game
        public event EventHandler<SpringLogEventArgs> PlayerSaid;

        public void ResignPlayer(string name)
        {
            if (IsRunning) talker.SendText($"/luarules resignteam {name}");
        }

        public void RunLocalScriptGame(string script, string engine)
        {
            Context = new SpringBattleContext();
            Context.SetForSelfHosting(engine);
            StartSpring(script);
        }

        public void SayGame(string text)
        {
            try
            {
                talker.SendText(text);
            }
            catch (NullReferenceException)
            {
                // do nothing: null reference is expected when game is not running
                // the property isRunning would be useful here if it didn't lie so much
            }
        }

        //public event EventHandler<> 
        /// <summary>
        ///     Data is true if exit was crash
        /// </summary>
        public event EventHandler<EventArgs<bool>> SpringExited;
        public event EventHandler SpringStarted;

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


        private void HandleSpecialMessages(Talker.SpringEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(e.Text) || !e.Text.StartsWith("SPRINGIE:")) return;

                int count;
                if (!gamePrivateMessages.TryGetValue(e.Text, out count)) count = 0;
                count++;
                gamePrivateMessages[e.Text] = count;
                if (count != 2) return; // only send if count matches 2 exactly

                var text = e.Text.Substring(9);
                if (text.StartsWith("READY:"))
                {
                    var name = text.Substring(6);
                    var entry = Context.ActualPlayers.FirstOrDefault(x => x.Name == name);
                    if (entry != null) entry.IsIngameReady = true;
                }
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
            if (sp != null) sp.LoseTime = isDead ? (int)DateTime.UtcNow.Subtract(Context.IngameStartTime ?? Context.StartTime).TotalSeconds : (int?)null;
        }

        private void ParseInfolog(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Trace.TraceWarning("Infolog is empty");
                return;
            }
            try
            {
                var missionVars = "";

                foreach (var cycleline in text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var line = cycleline;
                    var gameframe = 0;
                    if (line.StartsWith("[DedicatedServer]")) line = line.Replace("[DedicatedServer] ", "");

                    if (line.StartsWith("["))
                    {
                        var idx = line.IndexOf("] ");
                        if (idx > 0)
                        {
                            int.TryParse(line.Substring(1, idx - 1), out gameframe);
                            if (idx >= 0) line = line.Substring(idx + 2);
                        }
                    }

                    if (line.StartsWith("[AddGameSetupArchivesToVFS]")) line = line.Replace("[AddGameSetupArchivesToVFS] ", "");

                    // FIXME: why are these even null in the first place?
                    if (string.IsNullOrEmpty(LobbyStartContext.Map) && line.StartsWith("Using map", true, null)) LobbyStartContext.Map = line.Substring(10).Trim();

                    if (string.IsNullOrEmpty(LobbyStartContext.Mod) && line.StartsWith("Using game", true, null))
                    {
                        var archiveNameIndex = line.IndexOf("(archive", 11);
                        LobbyStartContext.Mod = line.Substring(11, archiveNameIndex - 11).Trim();
                        Trace.TraceInformation("Mod name: " + LobbyStartContext.Mod);
                    }

                    // obsolete? see above where [DedicatedServer] is pruned
                    if (line.StartsWith("recording demo")) Context.ReplayName = Path.GetFileName(line.Substring(15).Trim()); // 91.0
                    //else if (line.StartsWith("[DedicatedServer] recording demo")) demoFileName = Path.GetFileName(line.Substring(33).Trim());    // 95.0 and later

                    if (line.StartsWith("Using demofile", true, null)) return; // do nothing if its demo

                    if (line.StartsWith("GameID: ", true, null) && Context.EngineBattleID == null) Context.EngineBattleID = line.Substring(8).Trim();

                    if (line.StartsWith("STATS:")) Context.OutputExtras.Add(line.Substring(6));

                    if (line.Contains("SCORE: "))
                    {
                        var match = Regex.Match(line, "SCORE: ([^ ]+)");
                        if (match.Success)
                        {
                            // game score
                            var data = match.Groups[1].Value;
                            //Trace.TraceInformation("Score data (raw) : " + data);
                            data = Encoding.ASCII.GetString(Convert.FromBase64String(match.Groups[1].Value));
                            //Trace.TraceInformation("Score data (decoded) : " + data);
                            var parts = data.Split('/');
                            var score = 0;
                            if (parts.Length > 1)
                            {
                                score = Convert.ToInt32(parts[1]);
                                gameframe = Convert.ToInt32(parts[0]);
                            }
                            else score = Convert.ToInt32(data);
                            Context.MissionScore = score;
                            Context.MissionFrame = gameframe;
                        }
                    }
                    if (line.Contains("MISSIONVARS:"))
                    {
                        var match = Regex.Match(line, "MISSIONVARS: ([^ ]+)");
                        Context.MissionVars = match.Groups[1].Value.Trim();
                        Trace.TraceInformation($"Mission variables: {missionVars} (original line: {line})");
                    }

                    if (line.StartsWith("Cheating!", true, null) || line.StartsWith("Cheating is enabled!", true, null)) Context.IsCheating = true;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing spring log: {0}", ex);
            }
        }

        private void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            LogLineAdded(e.Data, true);
        }

        private void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Context.LogLines.AppendLine(e.Data);
            LogLineAdded(e.Data, false);
        }

        private void springProcess_Exited(object sender, EventArgs e)
        {
            Context.IsCrash = process.ExitCode != 0 && !Context.WasKilled;
            process.UnsubscribeEvents(this);
            try
            {
                if (!process.WaitForExit(2000)) process.Kill();
            }
            catch {}

            process = null;
            talker.UnsubscribeEvents(this);
            talker?.Close();
            talker = null;
            Thread.Sleep(1000);
            var logText = Context.LogLines.ToString();
            if (Context.IsHosting) ParseInfolog(logText);

            try
            {
                File.WriteAllText(Path.Combine(paths.WritableDirectory, $"infolog_{Context.EngineBattleID}.txt"), logText);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error saving infolog: {0}", ex);
            }

            GameExited = DateTime.Now;

            if (LobbyStartContext != null) foreach (var p in Context.ActualPlayers) p.IsIngame = false;

            SpringExited?.Invoke(this, new EventArgs<bool>(Context.IsCrash));
            AnySpringExited?.Invoke(this, new EventArgs<bool>(Context.IsCrash));
        }

        private void StartSpring(string script)
        {
            scriptPath = Path.GetTempFileName();
            File.WriteAllText(scriptPath, script);

            var optirun = Environment.GetEnvironmentVariable("OPTIRUN");

            process = new Process { StartInfo = { CreateNoWindow = true } };

            Environment.SetEnvironmentVariable("SPRING_DATADIR",
                paths.GetJoinedDataDirectoriesWithEngine(Context.EngineVersion),
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("SPRING_WRITEDIR", paths.WritableDirectory, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("SPRING_ISOLATED", paths.WritableDirectory, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("SPRING_NOCOLOR", "1", EnvironmentVariableTarget.Process);

            process.StartInfo.EnvironmentVariables["SPRING_DATADIR"] = paths.GetJoinedDataDirectoriesWithEngine(Context.EngineVersion);
            process.StartInfo.EnvironmentVariables["SPRING_WRITEDIR"] = paths.WritableDirectory;
            process.StartInfo.EnvironmentVariables["SPRING_ISOLATED"] = paths.WritableDirectory;
            process.StartInfo.EnvironmentVariables["SPRING_NOCOLOR"] = "1";

            var arg = new List<string>();

            if (string.IsNullOrEmpty(optirun))
            {
                if (Context.UseDedicatedServer)
                {
                    process.StartInfo.FileName = paths.GetDedicatedServerPath(Context.EngineVersion);
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.GetDedicatedServerPath(Context.EngineVersion));
                }
                else
                {
                    process.StartInfo.FileName = paths.GetSpringExecutablePath(Context.EngineVersion);
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.GetSpringExecutablePath(Context.EngineVersion));
                }
            }
            else
            {
                Trace.TraceInformation("Using optirun {0} to start the game (OPTIRUN env var defined)", optirun);
                process.StartInfo.FileName = optirun;
                arg.Add($"\"{paths.GetSpringExecutablePath(Context.EngineVersion)}\"");
            }

            arg.Add($"--config \"{paths.GetSpringConfigPath()}\"");
            if (paths.UseSafeMode) arg.Add("--safemode");
            arg.Add($"\"{scriptPath}\"");
            //Trace.TraceInformation("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            process.StartInfo.Arguments = string.Join(" ", arg);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            //process.StartInfo.RedirectStandardInput = true;
            process.Exited += springProcess_Exited;
            process.ErrorDataReceived += process_ErrorDataReceived;
            process.OutputDataReceived += process_OutputDataReceived;
            process.EnableRaisingEvents = true;

            gamePrivateMessages = new Dictionary<string, int>();
            Context.StartTime = DateTime.UtcNow;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            //process.StandardInput.Write(script);
            if (IsRunning)
            {
                SpringStarted?.Invoke(this, EventArgs.Empty);
                AnySpringStarted?.Invoke(this, EventArgs.Empty);
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
                        if (entry != null) entry.IsIngame = true;
                        PlayerJoined?.Invoke(this, new SpringLogEventArgs(e.PlayerName));
                        break;

                    case Talker.SpringEventType.PLAYER_LEFT:
                        entry = Context?.GetOrAddPlayer(e.PlayerName);
                        if (entry != null) entry.IsIngame = false;

                        if (e.Param == 0) PlayerDisconnected?.Invoke(this, new SpringLogEventArgs(e.PlayerName));
                        PlayerLeft?.Invoke(this, new SpringLogEventArgs(e.PlayerName));
                        break;

                    case Talker.SpringEventType.GAME_LUAMSG:
                        HandleSpecialMessages(e);
                        break;

                    case Talker.SpringEventType.PLAYER_CHAT:
                        if (e.Param == 255) HandleSpecialMessages(e);
                        else AddToLogs(e);

                        // only public chat
                        if (PlayerSaid != null && (e.Param == Talker.TO_EVERYONE || e.Param == Talker.TO_EVERYONE_LEGACY) &&
                            !string.IsNullOrEmpty(e.PlayerName)) PlayerSaid(this, new SpringLogEventArgs(e.PlayerName, e.Text));
                        break;

                    case Talker.SpringEventType.PLAYER_DEFEATED:
                        MarkPlayerDead(e.PlayerName, true);
                        if (PlayerLost != null) PlayerLost(this, new SpringLogEventArgs(e.PlayerName));
                        break;

                    case Talker.SpringEventType.SERVER_GAMEOVER:
                        foreach (var p in Context.ActualPlayers) p.IsIngame = false;

                        // set victory team for all allied with currently alive
                        foreach (var p in Context.ActualPlayers.Where(x => !x.IsSpectator && x.LoseTime == null))
                        {
                            foreach (var q in Context.ActualPlayers.Where(x => !x.IsSpectator && x.AllyNumber == p.AllyNumber))
                            {
                                q.IsVictoryTeam = true;
                            }
                        }

                        if (Context.IngameStartTime != null)
                        {
                            Context.GameEndedOk = true;
                            Context.Duration = (int)DateTime.UtcNow.Subtract(Context.IngameStartTime ?? Context.StartTime).TotalSeconds;

                            GameOver?.Invoke(this, new SpringLogEventArgs(e.PlayerName));
                        }
                        else Trace.TraceWarning("recieved GAMEOVER before STARTPLAYING!");
                        break;

                    case Talker.SpringEventType.PLAYER_READY:
                        if (e.Param == 1)
                        {
                            entry = Context.GetOrAddPlayer(e.PlayerName);
                            if (entry != null) entry.IsIngameReady = true;
                        }
                        break;

                    case Talker.SpringEventType.SERVER_STARTPLAYING:
                        Context.IngameStartTime = DateTime.UtcNow;
                        foreach (var p in Context.ActualPlayers.Where(x => !x.IsSpectator)) p.IsIngameReady = true;

                        BattleStarted(this, EventArgs.Empty);
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
                const int timeToWait = 180; // force start after 180s
                const int timeToWarn = 120; // warn people after 120s 

                if (Context.IsHosting && IsRunning && Context.IngameStartTime == null)
                {
                    if (timeSinceStart > timeToWait)
                    {
                        ForceStart();
                    }
                    else if (timeSinceStart > timeToWarn)
                    {
                        SayGame($"Game will be force started in {Math.Max(20, timeToWait - Math.Round(timeSinceStart))} seconds");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error checking start: {0}", ex);
            }
        }

        public class SpringBattleContext
        {
            public List<BattlePlayerResult> ActualPlayers = new List<BattlePlayerResult>();

            public int Duration;
            public string EngineBattleID;

            public string EngineVersion;

            public bool GameEndedOk;
            public DateTime? IngameStartTime;

            public string IpAddress;

            public bool IsCheating;

            public bool IsCrash;

            public bool IsHosting;
            public LobbyHostingContext LobbyStartContext = new LobbyHostingContext();

            public StringBuilder LogLines = new StringBuilder();
            public int MissionFrame;
            public int? MissionScore;
            public string MissionVars;
            public string MyPassword;
            public string MyUserName;

            public List<string> OutputExtras = new List<string>();
            public int Port;
            public string ReplayName;
            public DateTime StartTime;

            public bool UseDedicatedServer;
            public bool WasKilled;


            public BattlePlayerResult GetOrAddPlayer(string name)
            {
                var ret = ActualPlayers.FirstOrDefault(y => y.Name == name);
                if (ret == null)
                {
                    ret = new BattlePlayerResult(name);
                    ActualPlayers.Add(ret);
                }
                return ret;
            }


            public void SetForConnecting(string ip, int port, string myUser, string myPassword, string engineVersion)
            {
                UseDedicatedServer = false;
                IsHosting = false;
                IpAddress = ip;
                Port = port;
                MyUserName = myUser;
                MyPassword = myPassword;
                EngineVersion = engineVersion;
            }

            public void SetForHosting(LobbyHostingContext startContext,
                string ip,
                int? port,
                bool useDedicatedServer,
                string myUser,
                string myPassword)
            {
                LobbyStartContext = startContext;
                UseDedicatedServer = useDedicatedServer;
                EngineVersion = startContext.EngineVersion;
                IsHosting = true;
                IpAddress = ip ?? "127.0.0.1";
                Port = port ?? 8452;
                MyUserName = myUser;
                MyPassword = myPassword;
                ActualPlayers =
                    LobbyStartContext.Players.Select(
                        x =>
                            new BattlePlayerResult(x.Name)
                            {
                                AllyNumber = x.AllyID,
                                IsSpectator = x.IsSpectator,
                                IsVictoryTeam = false,
                                IsIngameReady = false,
                                IsIngame = false,
                            }).ToList();
            }


            public void SetForSelfHosting(string engineVersion)
            {
                UseDedicatedServer = false;
                IsHosting = true;
                EngineVersion = engineVersion;
                IpAddress = "127.0.0.1";
                Port = 8452;
            }
        }
    }
}