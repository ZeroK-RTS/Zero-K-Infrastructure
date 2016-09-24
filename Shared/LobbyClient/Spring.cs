#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using PlasmaShared;
using ZkData;

#endregion

namespace LobbyClient
{
    /// <summary>
    ///     represents one install location of spring game
    /// </summary>
    public class Spring : IDisposable
    {
        public delegate void LogLine(string text, bool isError);

        public static EventHandler AnySpringStarted;
        public static EventHandler<SpringBattleContext> AnySpringExited;

        private readonly SpringPaths paths;

        private StringBuilder logLines = new StringBuilder();

        private Process process;
        private string scriptPath;

        public SpringBattleContext Context { get; private set; }


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

        public Spring(SpringPaths springPaths)
        {
            paths = springPaths;
        }

        public void Dispose()
        {
            process?.UnsubscribeEvents(this);
            Context = null;
            scriptPath = null;
            process = null;
        }


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
                    if (!IsRunning) return;
                    Trace.TraceInformation("Terminating Spring process");
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


        public string HostGame(LobbyHostingContext startContext, string host, int port, string myName, string myPassword)
        {
            if (!File.Exists(paths.GetSpringExecutablePath(startContext.EngineVersion))) throw new ApplicationException($"Spring executable not found: {paths.GetSpringExecutablePath(startContext.EngineVersion)}");

            Context = new SpringBattleContext();
            Context.SetForHosting(startContext, host, port, myName, myPassword);

            if (!IsRunning)
            {
                Context.IsHosting = true;

                scriptPath = Utils.MakePath(paths.WritableDirectory, "script_" + myName + ".txt").Replace('\\', '/');

                var script = ScriptGenerator.GenerateHostScript(Context, 0);

                StartSpring(script);
                return script;
            }
            else Trace.TraceError("Spring already running");
            return null;
        }


        public event LogLine LogLineAdded = delegate { };


        public void RunLocalScriptGame(string script, string engine)
        {
            Context = new SpringBattleContext();
            Context.SetForSelfHosting(engine);
            StartSpring(script);
        }


        public event EventHandler<SpringBattleContext> SpringExited;
        public event EventHandler SpringStarted;

        public void WaitForExit()
        {
            process.WaitForExit();
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

                    if (line.StartsWith("GameID: ", true, null) && (Context.EngineBattleID == null)) Context.EngineBattleID = line.Substring(8).Trim();

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
            logLines.AppendLine(e.Data);
            LogLineAdded(e.Data, false);
        }

        private void springProcess_Exited(object sender, EventArgs e)
        {
            Context.IsCrash = (process.ExitCode != 0) && !Context.WasKilled;
            process.UnsubscribeEvents(this);
            try
            {
                if (!process.WaitForExit(2000)) process.Kill();
            }
            catch { }

            process = null;
            Thread.Sleep(1000);
            var logText = logLines.ToString();
            if (!Context.IsHosting) ParseInfolog(logText);

            try
            {
                File.WriteAllText(Path.Combine(paths.WritableDirectory, $"infolog_{Context.EngineBattleID}.txt"), logText);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error saving infolog: {0}", ex);
            }

            if (LobbyStartContext != null) foreach (var p in Context.ActualPlayers) p.IsIngame = false;

            SpringExited?.Invoke(this, Context);
            AnySpringExited?.Invoke(this, Context);
        }

        private void StartSpring(string script)
        {
            scriptPath = Path.GetTempFileName();
            File.WriteAllText(scriptPath, script);
            logLines.Clear();

            var optirun = Environment.GetEnvironmentVariable("OPTIRUN");

            process = new Process { StartInfo = { CreateNoWindow = true } };

            paths.SetDefaultEnvVars(process.StartInfo, Context.EngineVersion);

            var arg = new List<string>();

            if (string.IsNullOrEmpty(optirun))
            {
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

            Context.StartTime = DateTime.UtcNow;
            process.StartInfo.Arguments = string.Join(" ", arg);
            process.Exited += springProcess_Exited;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += process_ErrorDataReceived;
            process.OutputDataReceived += process_OutputDataReceived;
            process.EnableRaisingEvents = true;

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
    }
}