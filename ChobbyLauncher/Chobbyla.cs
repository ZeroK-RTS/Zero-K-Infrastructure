using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameAnalyticsSDK.Net;
using Neo.IronLua;
using PlasmaDownloader;
using PlasmaDownloader.Packages;
using ZkData;

namespace ChobbyLauncher
{
    public class Chobbyla
    {
        public PlasmaDownloader.PlasmaDownloader downloader;
        private string engine;
        private string chobbyTag;
        private string internalName;
        private bool isDev;
        private int loopbackPort;

        public SpringPaths paths;
        public SteamClientHelper Steam = new SteamClientHelper();

        public ulong InitialConnectLobbyID { get; private set; }
        public Process process { get; private set; }

        public IChobbylaProgress Progress { get; private set; } = new ProgressMeter();

        public string Status
        {
            get
            {
                return Progress.Status;
            }
            set
            {
                if (Progress.Status != value)
                {
                    Progress.Status = value;
                    Trace.TraceInformation(value);
                }
            }
        }

        public Chobbyla(string rootPath, string chobbyTagOverride, string engineOverride, ulong connectLobbyID)
        {
            InitialConnectLobbyID = connectLobbyID;
            paths = new SpringPaths(rootPath, false, true);
            chobbyTag = chobbyTagOverride ?? (GlobalConst.Mode == ModeType.Live ? "zkmenu:stable" : "zkmenu:test");
            isDev = (chobbyTag == "dev") || (chobbyTag == "chobby:dev") || (chobbyTag == "zkmenu:dev");
            engine = engineOverride;
            downloader = new PlasmaDownloader.PlasmaDownloader(null, paths);
        }


        public async Task<bool> Prepare()
        {
            try
            {
                PackageDownloader.Version ver = null;
                internalName = null;

                if (!isDev)
                {
                    if (!Debugger.IsAttached)
                    {
                        Status = "Checking for self-upgrade";
                        var selfUpdater = new SelfUpdater();
                        selfUpdater.ProgramUpdated += delegate
                        {
                            Process.Start(Application.ExecutablePath, string.Join(" ", Environment.GetCommandLineArgs().Skip(1)));
                            Environment.Exit(0);
                        };
                        var task = new Task<bool>(() => selfUpdater.CheckForUpdate());
                        task.Start();
                        await task;
                    }

                    if (!await downloader.DownloadFile("Checking for chobby update", DownloadType.RAPID, chobbyTag, Progress)) return false;
                    if (!await downloader.DownloadFile("Checking for game update", DownloadType.RAPID, "zk:stable", Progress)) return false;

                    ver = downloader.PackageDownloader.GetByTag(chobbyTag);
                    if (ver == null)
                    {
                        Status = "Rapid package appears to be corrupted, please clear the folder";
                        return false;
                    }

                    internalName = ver.InternalName;
                }
                else internalName = "Chobby $VERSION";

                engine = engine ?? QueryDefaultEngine() ?? ExtractEngineFromLua(ver) ?? GlobalConst.DefaultEngineOverride;

                if (!await downloader.DownloadFile("Downloading engine", DownloadType.ENGINE, engine, Progress)) return false;

                if (!await downloader.UpdateMissions(Progress))
                {
                    Trace.TraceWarning("Mission update has failed");
                    Status = "Error updating missions";
                }

                if (!isDev)
                {
                    Status = "Reseting configs and deploying AIs";
                    ConfigVersions.DeployAndResetConfigs(paths, ver);
                }

                Status = "Connecting to steam API";
                Steam.ConnectToSteam();

                Status = "Starting";
                var chobyl = new ChobbylaLocalListener(this);
                loopbackPort = chobyl.StartListening();

                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unexpected error: {0}", ex);
                GameAnalytics.AddErrorEvent(EGAErrorSeverity.Error, $"Unexpected error {Status}: {ex}");
                Status = "Unexpected error preparing chobby launch: " + ex.Message;
                return false;
            }
        }


        public Task<bool> Run()
        {
            return LaunchChobby(paths, internalName, engine, loopbackPort);
        }


        private dynamic ExtractEngineFromLua(PackageDownloader.Version ver)
        {
            if (ver != null)
            {
                var mi = ver.ReadFile(paths, "modinfo.lua");
                var lua = new Lua();
                var luaEnv = lua.CreateEnvironment();
                dynamic result = luaEnv.DoChunk(new StreamReader(mi), "dummy.lua");
                var engineVersion = result.engine;
                return engineVersion;
            }
            return null;
        }


        private async Task<bool> LaunchChobby(SpringPaths paths, string internalName, string engineVersion, int loopbackPort)
        {
            process = new Process { StartInfo = { CreateNoWindow = false, UseShellExecute = false } };

            paths.SetDefaultEnvVars(process.StartInfo, engineVersion);
            var widgetFolder = Path.Combine(paths.WritableDirectory); //, "LuaMenu", "Widgets");
            if (!Directory.Exists(widgetFolder)) Directory.CreateDirectory(widgetFolder);
            File.WriteAllText(Path.Combine(widgetFolder, "chobby_wrapper_port.txt"), loopbackPort.ToString());

            process.StartInfo.FileName = paths.GetSpringExecutablePath(engineVersion);
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.GetSpringExecutablePath(engineVersion));
            process.StartInfo.Arguments = $"--menu \"{internalName}\"";

            var tcs = new TaskCompletionSource<bool>();
            process.Exited += (sender, args) =>
            {
                var isCrash = process.ExitCode != 0;
                tcs.TrySetResult(!isCrash);
            };
            process.EnableRaisingEvents = true;
            process.Start();

            return await tcs.Task;
        }

        private string QueryDefaultEngine()
        {
            Status = "Querying default engine";
            try
            {
                return GlobalConst.GetContentService().GetDefaultEngine();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Querying default engine failed: {0}", ex);
                Status = "Querying default engine has failed";
            }
            return null;
        }

        public class ProgressMeter : IChobbylaProgress
        {
            public Download Download { get; set; }
            public string Status { get; set; }
        }
    }
}