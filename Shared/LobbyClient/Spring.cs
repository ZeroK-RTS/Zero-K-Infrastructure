#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using PlasmaShared;
using PlasmaShared.ModStats;
using ZkData;
using Timer = System.Timers.Timer;

#endregion

namespace LobbyClient
{
    public class SpringLogEventArgs : EventArgs
    {
        private readonly string line;
        private readonly string username;


        public string Line
        {
            get { return line; }
        }

        public string Username
        {
            get { return username; }
        }

        public SpringLogEventArgs(string username) : this(username, "") { }

        public SpringLogEventArgs(string username, string line)
        {
            this.line = line;
            this.username = username;
        }
    };


   
    /// <summary>
    /// represents one install location of spring game
    /// </summary>
    public class Spring
    {
        public const int MaxAllies = 16;
        public const int MaxTeams = 32;

        public delegate void LogLine(string text, bool isError);

        private Guid battleGuid;
        private BattleResult battleResult = new BattleResult();
        private readonly Dictionary<string, bool> connectedPlayers = new Dictionary<string, bool>();
        private bool gameEndedOk = false;
        private Dictionary<string, int> gamePrivateMessages = new Dictionary<string, int>();
        private bool isHosting;
        //private string lobbyPassword;
        //private string lobbyUserName;


        private readonly SpringPaths paths;


        private Process process;
        private string scriptPath;
        private readonly List<string> statsData = new List<string>();
        private Dictionary<string, BattlePlayerResult> statsPlayers = new Dictionary<string, BattlePlayerResult>();
        private Talker talker;
        private readonly Timer timer = new Timer(20000);
        private bool wasKilled = false;
        public int Duration
        {
            get { return battleResult.Duration; }
        }

        public DateTime GameEnded
        {
            get { return battleResult.StartTime.AddSeconds(battleResult.Duration).ToLocalTime(); }
        }
        public DateTime GameExited { get; private set; }

        public DateTime GameStarted
        {
            get { return battleResult.StartTime.ToLocalTime(); }
        }

        public bool IsRunning
        {
            get
            {
                try
                {
                    return (process != null && !process.HasExited);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error determining process state: {0}", ex);
                    return false;
                }
            }
        }

        public bool IsBattleOver { get; private set; }

        public StringBuilder LogLines = new StringBuilder();


        public ProcessPriorityClass ProcessPriority
        {
            set { if (IsRunning) process.PriorityClass = value; }
        }
        public BattleContext StartContext { get; private set; }
        public bool UseDedicatedServer;
        string lobbyUserName;
        string lobbyPassword;
        public event EventHandler BattleStarted = (sender, args) => { };

        public event EventHandler<SpringLogEventArgs> GameOver; // game has ended
        public event LogLine LogLineAdded = delegate { };
        public event EventHandler<SpringLogEventArgs> PlayerDisconnected;
        public event EventHandler<SpringLogEventArgs> PlayerJoined;
        public event EventHandler<SpringLogEventArgs> PlayerLeft;
        public event EventHandler<SpringLogEventArgs> PlayerLost; // player lost the game
        public event EventHandler<SpringLogEventArgs> PlayerSaid;
        //public event EventHandler<> 
        /// <summary>
        /// Data is true if exit was crash
        /// </summary>
        public event EventHandler<EventArgs<bool>> SpringExited;
        public event EventHandler SpringStarted;

        public Spring(SpringPaths springPaths)
        {
            paths = springPaths;
            timer.Elapsed += timer_Elapsed;
        }

        /// <summary>
        /// Adds user dynamically to running game - for security reasons add his script
        /// </summary>
        public void AddUser(string name, string scriptPassword)
        {
            if (IsRunning) talker.SendText(string.Format("/adduser {0} {1}", name, scriptPassword));
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
                    wasKilled = true;
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



        public bool IsPlayerReady(string name)
        {
            return statsPlayers[name].IsIngameReady;
        }

        public void Kick(string name)
        {
            SayGame("/kick " + name);
        }

        public void ResignPlayer(string name)
        {
            if (IsRunning) talker.SendText(string.Format("/luarules resignteam {0}", name));
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


        public string ConnectGame(string ip, int port, string myName, string myPassword = null)
        {
            battleResult = new BattleResult();
            lobbyUserName = myName;
            lobbyPassword = myPassword;
            isHosting = false;
            var script=  ScriptGenerator.GenerateConnectScript(ip, port, myName, myPassword);
            StartSpring(script);
            return script;
        }

   
        
        public string HostGame(BattleContext context, string host, int port, string myName = null, string myPassword = null )
        {
            if (!File.Exists(paths.Executable) && !File.Exists(paths.DedicatedServer)) throw new ApplicationException(string.Format("Spring or dedicated server executable not found: {0}, {1}", paths.Executable, paths.DedicatedServer));

            wasKilled = false;
            string script = null;

            if (!IsRunning)
            {
                gameEndedOk = false;
                IsBattleOver = false;
                //lobbyUserName = client.UserName;
                //lobbyPassword = client.UserPassword;
                battleResult = new BattleResult();

                talker = new Talker();
                talker.SpringEvent += talker_SpringEvent;
                isHosting = true;

                if (isHosting) scriptPath = Utils.MakePath(paths.WritableDirectory, "script_" + myName + ".txt").Replace('\\', '/');
                else scriptPath = Utils.MakePath(paths.WritableDirectory, "script.txt").Replace('\\', '/');

                statsPlayers.Clear();
                statsData.Clear();

                battleGuid = Guid.NewGuid();
                var service = GlobalConst.GetSpringieService();
                SpringBattleStartSetup startSetup = null;
                if (isHosting && GlobalConst.IsZkMod(context.Mod))
                {
                    try
                    {
                        StartContext = context;
                        startSetup = service.GetSpringBattleStartSetup(StartContext);
                        if (startSetup.BalanceTeamsResult != null)
                        {
                            StartContext.Players = startSetup.BalanceTeamsResult.Players;
                            StartContext.Bots = startSetup.BalanceTeamsResult.Bots;
                        }
                        connectedPlayers.Clear();
                        foreach (var p in StartContext.Players)
                        {
                            p.IsIngame = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error getting start setup: {0}", ex);
                    }

                    script = ScriptGenerator.GenerateHostScript(StartContext, startSetup, talker.LoopbackPort, battleGuid.ToString(), host,port, myName, myPassword);

                    statsPlayers = StartContext.Players.ToDictionary(x => x.Name,
                                                        x => new BattlePlayerResult
                                                                 {
                                                                     LobbyID = x.LobbyID,
                                                                     AllyNumber = x.AllyID,
                                                                     CommanderType = null,
                                                                     // todo commandertype
                                                                     IsSpectator = x.IsSpectator,
                                                                     IsVictoryTeam = false,
                                                                 });
                }
                if (isHosting) timer.Start();

                StartSpring(script);
                return script;
            }
            else Trace.TraceError("Spring already running");
            return null;
        }

        void StartSpring(string script)
        {
            scriptPath = Path.GetTempFileName();
            File.WriteAllText(scriptPath, script);

            LogLines = new StringBuilder();

            var optirun = Environment.GetEnvironmentVariable("OPTIRUN");

            process = new Process();
            process.StartInfo.CreateNoWindow = true;
            List<string> arg = new List<string>();

            if (string.IsNullOrEmpty(optirun)) {
                if (UseDedicatedServer) {
                    process.StartInfo.FileName = paths.DedicatedServer;
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.DedicatedServer);
                } else {
                    process.StartInfo.FileName = paths.Executable;
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.Executable);
                }
            } else {
                Trace.TraceInformation("Using optirun {0} to start the game (OPTIRUN env var defined)", optirun);
                process.StartInfo.FileName = optirun;
                arg.Add(string.Format("\"{0}\"", (paths.Executable)));
            }

            arg.Add(string.Format("--config \"{0}\"", paths.GetSpringConfigPath()));
            if (paths.SafeMode) arg.Add("--safemode");
            arg.Add(string.Format("\"{0}\"", scriptPath));
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
            battleResult.StartTime = DateTime.UtcNow;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            //process.StandardInput.Write(script);
            if (IsRunning && SpringStarted != null) SpringStarted(this, EventArgs.Empty);
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
                    BattlePlayerResult entry;
                    if (statsPlayers.TryGetValue(name, out entry)) entry.IsIngameReady = true;
                }
                if (text == "FORCE") ForceStart();

                statsData.Add(text);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while processing '{0}' :{1}", e.Text, ex);
            }
        }


        private void ParseInfolog(string text, bool isCrash)
        {
            if (string.IsNullOrEmpty(text))
            {
                Trace.TraceWarning("Infolog is empty");
                return;
            }
            try
            {
                var hasError = false;
                var isCheating = false;
                int? score = null;
                int scoreFrame = 0;
                string gameId = null;
                string demoFileName = null;
                string missionVars = "";

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
                    if (String.IsNullOrEmpty(StartContext.Map) && line.StartsWith("Using map", true, null)) StartContext.Map = line.Substring(10).Trim();

                    if (String.IsNullOrEmpty(StartContext.Mod) && line.StartsWith("Using game", true, null))
                    {
                        int archiveNameIndex = line.IndexOf("(archive", 11);
                        StartContext.Mod = line.Substring(11, archiveNameIndex - 11).Trim();
                        
                        Trace.TraceInformation("Mod name: " + StartContext.Mod);
                    }

                    // obsolete? see above where [DedicatedServer] is pruned
                    if (line.StartsWith("recording demo")) demoFileName = Path.GetFileName(line.Substring(15).Trim());  // 91.0
                    //else if (line.StartsWith("[DedicatedServer] recording demo")) demoFileName = Path.GetFileName(line.Substring(33).Trim());    // 95.0 and later

                    if (line.StartsWith("Using demofile", true, null)) return; // do nothing if its demo

                    if (line.StartsWith("GameID: ", true, null) && gameId == null) gameId = line.Substring(8).Trim();

                    if (line.StartsWith("STATS:")) statsData.Add(line.Substring(6));

                    if (line.Contains("SCORE: ") && !isCheating && StartContext.IsMission)
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
                            score = 0;
                            if (parts.Length > 1)
                            {
                                score = Convert.ToInt32(parts[1]);
                                gameframe = Convert.ToInt32(parts[0]);
                            }
                            else score = Convert.ToInt32(data);

                            scoreFrame = gameframe;
                        }
                    }
                    if (line.Contains("MISSIONVARS:") && StartContext.IsMission)
                    {
                        var match = Regex.Match(line, "MISSIONVARS: ([^ ]+)");
                        missionVars = match.Groups[1].Value.Trim();
                        Trace.TraceInformation(string.Format("Mission variables: {0} (original line: {1})", missionVars, line));
                    }

                    // obsolete, hanlded by pm messages 
                    //if (line.StartsWith("STATS:")) statsData.Add(line.Substring(6));

                    if (line.StartsWith("Cheating!", true, null) || line.StartsWith("Cheating is enabled!", true, null)) isCheating = true;

                    if (line.StartsWith("Error") || line.StartsWith("LuaRules") || line.StartsWith("Internal error") || line.StartsWith("LuaCOB") ||
                        (line.StartsWith("Failed to load") && !line.Contains("duplicate name"))) hasError = true;
                }
                if (score != null || !String.IsNullOrEmpty(missionVars))
                {
                    Trace.TraceInformation("Submitting score for mission " + StartContext.Mod);
                    try
                    {
                        var service = GlobalConst.GetContentService();
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                service.SubmitMissionScore(lobbyUserName, Utils.HashLobbyPassword(lobbyPassword), StartContext.Mod, score ?? 0, scoreFrame / 30,
                                    missionVars);
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Error sending score: {0}", ex);
                            }
                        });

                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(string.Format("Error sending mission score: {0}", ex));
                    }
                }


                // submit main stats
                if (!isCheating && !isCrash && gameEndedOk)
                {
                    if (isHosting)
                    {
                        var service = GlobalConst.GetSpringieService();
                        try
                        {
                            battleResult.EngineBattleID = gameId;
                            battleResult.ReplayName = demoFileName;

                            // set victory team for all allied with currently alive
                            foreach (var p in statsPlayers.Values.Where(x => !x.IsSpectator && x.LoseTime == null))
                            {
                                foreach (var q in statsPlayers.Values.Where(x => !x.IsSpectator && x.AllyNumber == p.AllyNumber))
                                {
                                    q.IsVictoryTeam = true;
                                }
                            }

                            if (StartContext != null)
                            {
                                var result = service.SubmitSpringBattleResult(StartContext, lobbyPassword, battleResult, statsPlayers.Values.ToList(), statsData);
                                if (result != null)
                                {
                                    foreach (var line in result.Split('\n'))
                                    {
                                        // HACK TODO client.Say(SayPlace.Battle, "", line, true);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error sending game result: {0}", ex);
                        }
                    }

                    if (statsData.Count > 1)
                    {
                        // must be more than 1 line - 1 is player list
                        var statsService = new StatsCollector { Proxy = null };
                        try
                        {
                            statsService.SubmitGameEx(gameId, StartContext.Mod, StartContext.Map, statsData.ToArray());
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error sending game stats: {0}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing spring log: {0}", ex);
            }
        }

        private void StatsMarkDead(string name, bool isDead)
        {
            BattlePlayerResult sp;
            if (statsPlayers.TryGetValue(name, out sp)) sp.LoseTime = isDead ? (int)DateTime.UtcNow.Subtract(battleResult.IngameStartTime ?? battleResult.StartTime).TotalSeconds : (int?)null;
        }

        private void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            LogLineAdded(e.Data, true);
        }

        private void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            LogLines.AppendLine(e.Data);
            LogLineAdded(e.Data, false);
        }

        private void springProcess_Exited(object sender, EventArgs e)
        {
            var isCrash = process.ExitCode != 0 && !wasKilled;
            process.UnsubscribeEvents(this);
            try
            {
                if (!process.WaitForExit(2000)) process.Kill();
            }
            catch { }

            process = null;
            talker?.Close();
            talker = null;
            Thread.Sleep(1000);
            var logText = LogLines.ToString();
            if (isHosting) ParseInfolog(logText, isCrash);

            try
            {
                File.WriteAllText(Path.Combine(paths.WritableDirectory, string.Format("infolog_{0}.txt", battleResult.EngineBattleID)), logText);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error saving infolog: {0}", ex);
            }

            GameExited = DateTime.Now;

            if (StartContext != null) foreach (var p in StartContext.Players) p.IsIngame = false;
            IsBattleOver = true;

            if (SpringExited != null) SpringExited(this, new EventArgs<bool>(isCrash));
        }


        private void talker_SpringEvent(object sender, Talker.SpringEventArgs e)
        {
            //this.client.Say(SayPlace.Battle, "",string.Format("type:{0} param:{1} player:{2}-{3} text:{4}",e.EventType.ToString(), e.Param,e.PlayerNumber, e.PlayerName, e.Text),false);
            try
            {
                switch (e.EventType)
                {
                    case Talker.SpringEventType.PLAYER_JOINED:
                        if (StartContext != null)
                        {
                            foreach (var p in StartContext.Players.Where(x => x.Name == e.PlayerName))
                            {
                                connectedPlayers[p.Name] = true;
                                p.IsIngame = true;
                            }
                        }
                        if (PlayerJoined != null) PlayerJoined(this, new SpringLogEventArgs(e.PlayerName));
                        break;

                    case Talker.SpringEventType.PLAYER_LEFT:
                        if (StartContext != null)
                        {
                            foreach (var p in StartContext.Players.Where(x => x.Name == e.PlayerName))
                            {
                                connectedPlayers[p.Name] = false;
                                p.IsIngame = false;
                            }
                        }
                        if (e.Param == 0 && PlayerDisconnected != null) PlayerDisconnected(this, new SpringLogEventArgs(e.PlayerName));
                        if (PlayerLeft != null) PlayerLeft(this, new SpringLogEventArgs(e.PlayerName));

                        break;

                    case Talker.SpringEventType.GAME_LUAMSG:
                        HandleSpecialMessages(e);
                        break;

                    case Talker.SpringEventType.PLAYER_CHAT:
                        if (e.Param == 255) HandleSpecialMessages(e);

                        // only public chat
                        if (PlayerSaid != null && (e.Param == Talker.TO_EVERYONE || e.Param == Talker.TO_EVERYONE_LEGACY) && !string.IsNullOrEmpty(e.PlayerName)) PlayerSaid(this, new SpringLogEventArgs(e.PlayerName, e.Text));
                        break;

                    case Talker.SpringEventType.PLAYER_DEFEATED:
                        StatsMarkDead(e.PlayerName, true);
                        if (PlayerLost != null) PlayerLost(this, new SpringLogEventArgs(e.PlayerName));
                        break;

                    case Talker.SpringEventType.SERVER_GAMEOVER:
                        if (StartContext != null) foreach (var p in StartContext.Players) p.IsIngame = false;
                        IsBattleOver = true;

                        if (battleResult.IngameStartTime != null)
                        {
                            gameEndedOk = true;
                            battleResult.Duration = (int)DateTime.UtcNow.Subtract(battleResult.IngameStartTime ?? battleResult.StartTime).TotalSeconds;
                            if (GameOver != null) GameOver(this, new SpringLogEventArgs(e.PlayerName));
                        }
                        else
                        {
                            //gameover before gamestart
                            Trace.TraceWarning("recieved GAMEOVER before STARTPLAYING!");
                        }

                        break;

                    case Talker.SpringEventType.PLAYER_READY:
                        if (e.Param == 1) statsPlayers[e.PlayerName].IsIngameReady = true;
                        break;

                    case Talker.SpringEventType.SERVER_STARTPLAYING:
                        battleResult.IngameStartTime = DateTime.UtcNow;
                        foreach (var p in statsPlayers)
                        {
                            p.Value.IsIngameReady = true;
                        }

                        foreach (var p in StartContext.Players)
                        {
                            bool state;
                            if (!connectedPlayers.TryGetValue(p.Name, out state) || !state) p.IsIngame = false;
                        }
                        BattleStarted(this, EventArgs.Empty);
                        break;

                    case Talker.SpringEventType.SERVER_QUIT:
                        if (StartContext != null) foreach (var p in StartContext.Players) p.IsIngame = false;
                        IsBattleOver = true;
                        //Program.main.AutoHost.SayBattle("dbg quit ");
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
                var timeSinceStart = DateTime.UtcNow.Subtract(battleResult.StartTime).TotalSeconds;
                const int timeToWait = 180; // force start after 180s
                const int timeToWarn = 120; // warn people after 120s 

                if (isHosting && IsRunning && battleResult.IngameStartTime == null)
                {
                    if (timeSinceStart > timeToWait)
                    {
                        ForceStart();
                    }
                    else if (timeSinceStart > timeToWarn)
                    {
                        SayGame(string.Format("Game will be force started in {0} seconds", Math.Max(20, timeToWait - Math.Round(timeSinceStart))));
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error checking start: {0}", ex);
            }
        }

        public void WaitForExit()
        {
            process.WaitForExit();
        }

        public void RunLocalScriptGame(object script)
        {
            throw new NotImplementedException();
        }
    }
}
