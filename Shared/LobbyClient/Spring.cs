#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;
using PlasmaShared;
using PlasmaShared.ContentService;
using ZkData;
using Timer = System.Timers.Timer;

#endregion

namespace LobbyClient
{
    public class SpringLogEventArgs: EventArgs
    {
        readonly string line;
        readonly string username;


        public string Line { get { return line; } }

        public string Username { get { return username; } }

        public SpringLogEventArgs(string username): this(username, "") {}

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

        Guid battleGuid;
        BattleResult battleResult = new BattleResult();
        TasClient client;
        bool gameEndedOk = false;
        Dictionary<string, int> gamePrivateMessages = new Dictionary<string, int>();
        bool isHosting;
        string lobbyPassword;
        string lobbyUserName;


        readonly SpringPaths paths;


        Process process;
        string scriptPath;
        readonly List<string> statsData = new List<string>();
        Dictionary<string, BattlePlayerResult> statsPlayers = new Dictionary<string, BattlePlayerResult>();
        Talker talker;
        readonly Timer timer = new Timer(20000);
        bool wasKilled = false;
        public int Duration { get { return battleResult.Duration; } }

        public DateTime GameEnded { get { return battleResult.StartTime.AddSeconds(battleResult.Duration).ToLocalTime(); } }

        public DateTime GameStarted { get { return battleResult.StartTime.ToLocalTime(); } }

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
        public StringBuilder LogLines = new StringBuilder();


        public ProcessPriorityClass ProcessPriority { set { if (IsRunning) process.PriorityClass = value; } }
        public bool UseDedicatedServer;

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

        public Spring(SpringPaths springPaths, AutohostMode autohostMode = AutohostMode.GameTeams)
        {
            paths = springPaths;
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
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
                    process.WaitForExit(2000);
                    if (!IsRunning) return;
                    wasKilled = true;
                    process.Kill();
                    ;
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


        public void SayGame(string text)
        {
            if (IsRunning) talker.SendText(text);
        }

        /// <summary>
        /// Starts spring game
        /// </summary>
        /// <param name="client">tasclient to get current battle from</param>
        /// <param name="priority">spring process priority</param>
        /// <param name="affinity">spring process cpu affinity</param>
        /// <param name="scriptOverride">if set, overrides generated script with supplied one</param>
        /// <param name="userName">lobby user name - used to submit score</param>
        /// <param name="passwordHash">lobby password hash - used to submit score</param>
        /// <returns>generates script</returns>
        public string StartGame(TasClient client, ProcessPriorityClass? priority, int? affinity, string scriptOverride)
        {
            if (!File.Exists(paths.Executable) && !File.Exists(paths.DedicatedServer)) throw new ApplicationException("Spring or dedicated server executable not found");

            this.client = client;
            wasKilled = false;

            if (!IsRunning)
            {
                gameEndedOk = false;
                lobbyUserName = client.UserName;
                lobbyPassword = client.UserPassword;
                battleResult = new BattleResult();

                talker = new Talker();
                talker.SpringEvent += talker_SpringEvent;
                isHosting = client != null && client.MyBattle != null && client.MyBattle.Founder.Name == client.MyUser.Name;

                if (isHosting) scriptPath = Utils.MakePath(paths.WritableDirectory, "script_" + client.MyBattle.Founder + ".txt").Replace('\\', '/');
                else scriptPath = Utils.MakePath(paths.WritableDirectory, "script.txt").Replace('\\', '/');

                statsPlayers.Clear();
                statsData.Clear();

                string script;
                if (!string.IsNullOrEmpty(scriptOverride))
                {
                    battleResult.IsMission = true;
                    isHosting = false;
                    script = scriptOverride;
                }
                else
                {
                    List<UserBattleStatus> players;
                    battleGuid = Guid.NewGuid();
                    var service = new ContentService() { Proxy = null };
                    SpringBattleStartSetup startSetup = null;
                    if (isHosting && GlobalConst.IsZkMod(client.MyBattle.ModName))
                    {
                        try
                        {
                            startSetup = service.GetSpringBattleStartSetup(client.MyUser.Name,
                                                                           client.MyBattle.MapName,
                                                                           client.MyBattle.ModName,
                                                                           client.MyBattle.Users.Select(
                                                                               x =>
                                                                               new BattleStartSetupPlayer()
                                                                               {
                                                                                   AccountID = x.LobbyUser.LobbyID,
                                                                                   AllyTeam = x.AllyNumber,
                                                                                   IsSpectator = x.IsSpectator
                                                                               }).ToArray(),
                                                                           AutohostMode.GameTeams);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error getting start setup: {0}", ex);
                        }
                    }

                    script = client.MyBattle.GenerateScript(out players, client.MyUser, talker.LoopbackPort, battleGuid.ToString(), startSetup);
                    battleResult.IsMission = client.MyBattle.IsMission;
                    battleResult.IsBots = client.MyBattle.Bots.Any();
                    battleResult.Title = client.MyBattle.Title;
                    battleResult.Mod = client.MyBattle.ModName;
                    battleResult.Map = client.MyBattle.MapName;
                    battleResult.EngineVersion = paths.SpringVersion;
                    talker.SetPlayers(players);
                    statsPlayers = players.ToDictionary(x => x.Name,
                                                        x =>
                                                        new BattlePlayerResult
                                                        {
                                                            AccountID = x.LobbyUser.LobbyID,
                                                            AllyNumber = x.AllyNumber,
                                                            CommanderType = null,
                                                            // todo commandertype
                                                            IsSpectator = x.IsSpectator,
                                                            IsVictoryTeam = false,
                                                            Rank = x.LobbyUser.Rank,
                                                        });
                }
                if (isHosting) timer.Start();

                File.WriteAllText(scriptPath, script);

                LogLines = new StringBuilder();

                process = new Process();
                process.StartInfo.CreateNoWindow = true;

                process.StartInfo.Arguments += string.Format("--config \"{0}\"", paths.GetSpringConfigPath());
                process.StartInfo.EnvironmentVariables["SPRING_DATADIR"] = paths.WritableDirectory;

                if (UseDedicatedServer)
                {
                    process.StartInfo.EnvironmentVariables["SPRING_ISOLATED"] = paths.WritableDirectory;
                    process.StartInfo.FileName = paths.DedicatedServer;
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.DedicatedServer);
                    process.StartInfo.Arguments += string.Format(" -i --isolation-dir \"{0}\"", paths.WritableDirectory);
                }
                else
                {
                    process.StartInfo.FileName = paths.Executable;
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.Executable);
                }

                process.StartInfo.Arguments += string.Format(" \"{0}\"", scriptPath);
                //Trace.TraceInformation("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Exited += springProcess_Exited;
                process.ErrorDataReceived += process_ErrorDataReceived;
                process.OutputDataReceived += process_OutputDataReceived;
                process.EnableRaisingEvents = true;

                gamePrivateMessages = new Dictionary<string, int>();
                battleResult.StartTime = DateTime.UtcNow;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                if (IsRunning && SpringStarted != null) SpringStarted(this, EventArgs.Empty);

                Utils.StartAsync(() =>
                    {
                        Thread.Sleep(1000);
                        try
                        {
                            if (priority != null) process.PriorityClass = priority.Value;
                            if (affinity != null) process.ProcessorAffinity = (IntPtr)affinity.Value;
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceWarning("Error setting spring process affinity: {0}", ex);
                        }
                    });

                return script;
            }
            else Trace.TraceError("Spring already running");
            return null;
        }

        bool FindAndExtractDemoInfo(ref string gameId, out string demoFileName)
        {
            demoFileName = null;

            var candidates = new List<FileInfo>();
            var di = new DirectoryInfo(Path.Combine(paths.WritableDirectory, "demos"));

            if (di.Exists)
            {
                candidates.AddRange(
                    di.GetFiles().Where(
                        x =>
                        x.CreationTimeUtc > GameStarted.ToUniversalTime().AddMinutes(-70) &&
                        x.CreationTimeUtc < GameEnded.ToUniversalTime().AddMinutes(70)));
            }
            di = new DirectoryInfo(Path.Combine(paths.UnitSyncDirectory, "demos"));
            if (di.Exists)
            {
                candidates.AddRange(
                    di.GetFiles().Where(
                        x =>
                        x.CreationTimeUtc > GameStarted.ToUniversalTime().AddMinutes(-70) &&
                        x.CreationTimeUtc < GameEnded.ToUniversalTime().AddMinutes(70)));
            }
            //Console.WriteLine("Candidates: " + candidates.Count);
            foreach (var file in candidates)
            {
                try
                {
                    var buf = new byte[120000];
                    var read = 0;
                    using (var stream = file.OpenRead()) read = stream.Read(buf, 0, buf.Length);

                    if (read > 113)
                    {
                        var script = Encoding.ASCII.GetString(buf, 112, read - 112 - 1);
                        if (script.Contains(battleGuid.ToString()))
                        {
                            // found correct 
                            var hash = new Hash(buf, 40);
                            gameId = hash.ToString();
                            demoFileName = file.Name;
                            return true;
                        }
                    }
                }
                catch {}
            }
            return false;
        }


        void HandleSpecialMessages(Talker.SpringEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(e.Text) || !e.Text.StartsWith("SPRINGIE:")) return;

                int count;
                if (!gamePrivateMessages.TryGetValue(e.Text, out count)) count = 0;
                count++;
                gamePrivateMessages[e.Text] = count;
                if (count != 2) return; // only send if count matches 2 exactly

                statsData.Add(e.Text.Substring(9));
            }

            catch (Exception ex)
            {
                Trace.TraceError("Error while processing '{0}' :{1}", e.Text, ex);
            }
        }


        void ParseInfolog(string text, bool isCrash)
        {
            if (string.IsNullOrEmpty(text))
            {
                Trace.TraceWarning("Infolog is empty");
                return;
            }
            try
            {
                var hasError = false;
                var modName = battleResult.Mod;
                var mapName = battleResult.Map;
                var isCheating = false;
                string gameId = null;
                string demoFileName = null;

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

                    if (mapName == null && line.StartsWith("using map")) mapName = line.Substring(10).Trim();

                    if (modName == null && line.StartsWith("using mod")) modName = line.Substring(10).Trim();

                    // todo uncomment when engine/dedi fixed if (line.StartsWith("recording demo")) demoFileName = Path.GetFileName(line.Substring(15).Trim());

                    if (line.StartsWith("Using demofile")) return; // do nothing if its demo

                    if (line.StartsWith("GameID: ") && gameId == null) gameId = line.Substring(8).Trim();

                    if (line.StartsWith("STATS:")) statsData.Add(line.Substring(6));

                    if (line.StartsWith("LuaRules: >> ID: ") && !isCheating && battleResult.IsMission)
                    {
                        // game score
                        var data = Encoding.ASCII.GetString(Convert.FromBase64String(line.Substring(17)));
                        var parts = data.Split('/');
                        var score = 0;
                        if (parts.Length > 1)
                        {
                            score = Convert.ToInt32(parts[1]);
                            gameframe = Convert.ToInt32(parts[0]);
                        }
                        else score = Convert.ToInt32(data);

                        using (var service = new ContentService { Proxy = null })
                        {
                            service.SubmitMissionScoreCompleted += (s, e) =>
                                {
                                    if (e.Error != null)
                                    {
                                        if (e.Error is WebException) Trace.TraceWarning("Error sending score: {0}", e.Error);
                                        else Trace.TraceError("Error sending score: {0}", e.Error);
                                    }
                                };
                            service.SubmitMissionScoreAsync(lobbyUserName, Utils.HashLobbyPassword(lobbyPassword), modName, score, gameframe/30);
                        }
                    }

                    // obsolete, hnalded by pm messages if (line.StartsWith("STATS:")) statsData.Add(line.Substring(6));

                    if (line.StartsWith("Cheating!")) isCheating = true;

                    if (line.StartsWith("Error") || line.StartsWith("LuaRules") || line.StartsWith("Internal error") || line.StartsWith("LuaCOB") ||
                        (line.StartsWith("Failed to load") && !line.Contains("duplicate name"))) hasError = true;
                }

                var modOk = GlobalConst.IsZkMod(modName);

                // submit main stats
                if (!isCheating && !isCrash && modOk && gameEndedOk)
                {
                    if (isHosting)
                    {
                        var service = new ContentService() { Proxy = null };
                        try
                        {
                            if (string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(demoFileName)) FindAndExtractDemoInfo(ref gameId, out demoFileName);

                            battleResult.EngineBattleID = gameId;
                            battleResult.ReplayName = demoFileName;

                            // set victory team for all allied with currently alive
                            foreach (var p in statsPlayers.Values.Where(x => !x.IsSpectator && x.LoseTime == null)) foreach (var q in statsPlayers.Values.Where(x => !x.IsSpectator && x.AllyNumber == p.AllyNumber)) q.IsVictoryTeam = true;

                            var result = service.SubmitSpringBattleResult(lobbyUserName,
                                                                          lobbyPassword,
                                                                          battleResult,
                                                                          statsPlayers.Values.ToArray(),
                                                                          statsData.ToArray());
                            if (result != null) foreach (var line in result.Split('\n')) client.Say(TasClient.SayPlace.Battle, "", line, true);
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
                            statsService.SubmitGameEx(gameId, modName, mapName, statsData.ToArray());
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

        void StatsMarkDead(string name, bool isDead)
        {
            BattlePlayerResult sp;
            if (statsPlayers.TryGetValue(name, out sp)) sp.LoseTime = isDead ? (int)DateTime.UtcNow.Subtract(battleResult.StartTime).TotalSeconds : (int?)null;
        }

        void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            LogLineAdded(e.Data, true);
        }

        void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            LogLines.AppendLine(e.Data);
            LogLineAdded(e.Data, false);
        }

        void springProcess_Exited(object sender, EventArgs e)
        {
            var isCrash = process.ExitCode != 0 && !wasKilled;
            try
            {
                if (!process.WaitForExit(2000)) process.Kill();
            }
            catch {}

            process = null;
            talker.Close();
            talker = null;
            Thread.Sleep(1000);
            var logText = LogLines.ToString();
            ParseInfolog(logText, isCrash);

            try
            {
                File.WriteAllText(Path.Combine(paths.WritableDirectory, string.Format("infolog_{0}.txt", battleResult.EngineBattleID)), logText);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error saving infolog: {0}", ex);
            }

            if (SpringExited != null) SpringExited(this, new EventArgs<bool>(isCrash));
        }


        void talker_SpringEvent(object sender, Talker.SpringEventArgs e)
        {
            //this.client.Say(TasClient.SayPlace.Battle, "",string.Format("type:{0} param:{1} player:{2}-{3} text:{4}",e.EventType.ToString(), e.Param,e.PlayerNumber, e.PlayerName, e.Text),false);
            try
            {
                switch (e.EventType)
                {
                    case Talker.SpringEventType.PLAYER_JOINED:
                        if (PlayerJoined != null) PlayerJoined(this, new SpringLogEventArgs(e.PlayerName));
                        break;

                    case Talker.SpringEventType.PLAYER_LEFT:
                        if (e.Param == 0 && PlayerDisconnected != null) PlayerDisconnected(this, new SpringLogEventArgs(e.PlayerName));
                        if (PlayerLeft != null) PlayerLeft(this, new SpringLogEventArgs(e.PlayerName));

                        break;

                    case Talker.SpringEventType.GAME_LUAMSG:
                        HandleSpecialMessages(e);
                        break;

                    case Talker.SpringEventType.PLAYER_CHAT:
                        if (e.Param == 255) HandleSpecialMessages(e);

                        // only public chat
                        if (PlayerSaid != null && (e.Param == Talker.TO_EVERYONE || e.Param == Talker.TO_EVERYONE_LEGACY) &&
                            !string.IsNullOrEmpty(e.PlayerName)) PlayerSaid(this, new SpringLogEventArgs(e.PlayerName, e.Text));
                        break;

                    case Talker.SpringEventType.PLAYER_DEFEATED:
                        StatsMarkDead(e.PlayerName, true);
                        if (PlayerLost != null) PlayerLost(this, new SpringLogEventArgs(e.PlayerName));
                        break;

                    case Talker.SpringEventType.SERVER_GAMEOVER:
                        gameEndedOk = true;
                        battleResult.Duration = (int)DateTime.UtcNow.Subtract(battleResult.StartTime).TotalSeconds;
                        if (GameOver != null) GameOver(this, new SpringLogEventArgs(e.PlayerName));
                        break;

                    case Talker.SpringEventType.PLAYER_READY:
                        if (e.Param == 1) statsPlayers[e.PlayerName].IsIngameReady = true;
                        break;

                    case Talker.SpringEventType.SERVER_STARTPLAYING:
                        battleResult.IngameStartTime = DateTime.UtcNow;
                        break;

                    case Talker.SpringEventType.SERVER_QUIT:
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

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (isHosting && IsRunning && battleResult.IngameStartTime == null)
                {
                    // force start after 180s
                    if (DateTime.UtcNow.Subtract(battleResult.StartTime).TotalSeconds > 240)
                    {
                        foreach (var kvp in statsPlayers.Where(x => !x.Value.IsIngameReady && !x.Value.IsSpectator))
                        {
                            Kick(kvp.Key);
                            client.ForceSpectator(kvp.Key);
                        }
                        ForceStart();
                    }
                    else if (DateTime.UtcNow.Subtract(battleResult.StartTime).TotalSeconds > 120)
                    {
                        //warn people after 60s 
                        foreach (var kvp in statsPlayers.Where(x => !x.Value.IsIngameReady && !x.Value.IsSpectator))
                        {
                            client.Ring(kvp.Key);
                            client.Say(TasClient.SayPlace.User, kvp.Key, "Please ready up ingame, game starting soon", false);
                        }
                        SayGame(string.Format("Game will be force started in {0} seconds",
                                              Math.Max(20, 240 - Math.Round(DateTime.UtcNow.Subtract(battleResult.StartTime).TotalSeconds))));
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error checking start: {0}", ex);
            }
        }
    }
}