using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Neo.IronLua;
using PlasmaDownloader;
using PlasmaDownloader.Packages;
using ZkData;

namespace ChobbyLauncher
{
    public class Chobbyla
    {
        private static List<string> defaultConfigs = new List<string>()
        {
            "LuaMenu/configs/gameConfig/zk/defaultSettings/lups.cfg",
            "LuaMenu/configs/gameConfig/zk/defaultSettings/springsettings.cfg"
        };
        private string chobbyTag;
        private string engine;
        private bool isDev;

        private SpringPaths paths;
        private string internalName;
        private int loopbackPort;
        public Download Download { get; private set; }
        public string Status { get; private set; }


        public Chobbyla(string rootPath, string chobbyTagOverride, string engineOverride)
        {
            paths = new SpringPaths(rootPath, false);
            chobbyTag = chobbyTagOverride ?? (GlobalConst.Mode == ModeType.Live ? "chobby:stable" : "chobby:test");
            isDev = chobbyTag == "dev" || chobbyTag == "chobby:dev";
            engine = engineOverride;
        }

        

        public Task Run()
        {
            return LaunchChobby(paths, internalName, engine, loopbackPort);
        }

        public async Task<bool> Prepare()
        {
            try
            {
                var downloader = new PlasmaDownloader.PlasmaDownloader(new SpringScanner(paths) { WatchingEnabled = false, UseUnitSync = false },
                     paths);

                PackageDownloader.Version ver = null;
                internalName = null;

                if (!isDev)
                {
                    if (!Debugger.IsAttached)
                    {
                        Status = "Checking for self-upgrade";
                        var selfUpdater = new SelfUpdater();
                        selfUpdater.ProgramUpdated += delegate { Application.Restart(); };
                        var task = new Task<bool>(() => selfUpdater.CheckForUpdate());
                        task.Start();
                        await task;
                    }


                    Status = "Updating rapid packages";
                    await downloader.PackageDownloader.LoadMasterAndVersions();
                    Status = "Checking for chobby update";
                    Download = downloader.GetResource(DownloadType.MOD, chobbyTag);
                    var asTask = Download?.WaitHandle.AsTask(TimeSpan.FromMinutes(20));
                    if (asTask != null) await asTask;
                    if (Download?.IsComplete == false)
                    {
                        Status = $"Download of {Download.Name} has failed";
                        return false;
                    }

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


                Status = "Downloading engine";
                Download = downloader.GetResource(DownloadType.ENGINE, engine);
                var engDlTask = Download?.WaitHandle.AsTask(TimeSpan.FromMinutes(20));
                if (engDlTask != null) await engDlTask;
                if (Download?.IsComplete == false)
                {
                    Status = $"Download of engine {Download.Name} has failed";
                    return false;
                }

                if (!isDev)
                {
                    Status = "Extracting default configs";
                    ExtractDefaultConfigs(paths, ver);
                }
                
                Status = "Starting";

                var listener = ChobbylaLocalListener.Init();
                var chobyl = new ChobbylaLocalListener();
                chobyl.Listen(listener);

                IPEndPoint endPoint = (IPEndPoint)listener.Server.LocalEndPoint;
                loopbackPort = endPoint.Port;

                
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unexpected error: {0}", ex);
                Status = "Unexpected error preparing chobby launch: " + ex.Message;
                return false;
            }
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

        private static void ExtractDefaultConfigs(SpringPaths paths, PackageDownloader.Version ver)
        {
            if (ver != null)
            {
                foreach (var f in defaultConfigs)
                {
                    var target = Path.Combine(paths.WritableDirectory, Path.GetFileName(f));
                    if (!File.Exists(target))
                    {
                        var content = ver.ReadFile(paths, f);
                        if (content != null) File.WriteAllBytes(target, content.ToArray());
                    }
                }
            }
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


        private async Task LaunchChobby(SpringPaths paths, string internalName, string engineVersion, int loopbackPort)
        {
            var process = new Process { StartInfo = { CreateNoWindow = true, UseShellExecute = false } };

            paths.SetDefaultEnvVars(process.StartInfo, engineVersion);
            var widgetFolder = Path.Combine(paths.WritableDirectory, "LuaMenu", "Widgets");
            if (!Directory.Exists(widgetFolder)) Directory.CreateDirectory(widgetFolder);
            File.WriteAllText(Path.Combine(widgetFolder, "chobby_wrapper_port.txt"), loopbackPort.ToString());

            process.StartInfo.FileName = paths.GetSpringExecutablePath(engineVersion);
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.GetSpringExecutablePath(engineVersion));
            process.StartInfo.Arguments = $"--menu \"{internalName}\"";


            var tcs = new TaskCompletionSource<bool>();
            process.Exited += (sender, args) => tcs.TrySetResult(true);
            process.EnableRaisingEvents = true;
            process.Start();

            await tcs.Task;
        }
    }
}