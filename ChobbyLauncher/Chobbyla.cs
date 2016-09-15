using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private SpringPaths paths;
        public Download Download { get; private set; }
        public string Status { get; private set; }


        public Chobbyla(string rootPath, string chobbyTagOverride, string engineOverride)
        {
            paths = new SpringPaths(rootPath, false);
            chobbyTag = chobbyTagOverride ?? "chobby:stable";
            engine = engineOverride;
        }


        public async Task<bool> Run()
        {
            try
            {
                Status = "Checking for self-upgrade";

                var selfUpdater = new SelfUpdater();
                selfUpdater.ProgramUpdated += delegate { Application.Restart(); };
                var task = new Task<bool>(() => selfUpdater.CheckForUpdate());
                task.Start();
                await task;

                Status = "Checking for chobby update";

                var downloader = new PlasmaDownloader.PlasmaDownloader(new SpringScanner(paths) { WatchingEnabled = false, UseUnitSync = false },
                    paths);
                Download = downloader.GetResource(DownloadType.MOD, chobbyTag);

                var asTask = Download?.WaitHandle.AsTask(TimeSpan.FromMinutes(20));
                if (asTask != null) await asTask;
                if (Download?.IsComplete == false)
                {
                    Status = $"Download of {Download.Name} has failed";
                    return false;
                }

                var ver = downloader.PackageDownloader.GetByTag(chobbyTag);
                if (ver != null)
                {
                    if (engine == null)
                    {
                        Status = "Querying default engine";
                        try
                        {
                            engine = GlobalConst.GetContentService().GetDefaultEngine();
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Querying default engine failed: {0}", ex);
                            Status = "Querying default engine has failed";
                        }
                        if (engine == null) engine = ExtractEngineFromLua(ver);
                        if (engine == null) engine = GlobalConst.DefaultEngineOverride;
                    }

                    Status = "Downloading engine";
                    Download = downloader.GetResource(DownloadType.ENGINE, engine);
                    var engDlTask = Download?.WaitHandle.AsTask(TimeSpan.FromMinutes(20));
                    if (engDlTask != null) await engDlTask;
                    if (Download?.IsComplete == false)
                    {
                        Status = $"Download of engine {Download.Name} has failed";
                        return false;
                    }

                    Status = "Extracting default configs";
                    ExtractDefaultConfigs(paths, ver);

                    Status = "Starting";
                    LaunchChobby(paths, ver.InternalName, engine);
                    return true;
                }
                else
                {
                    Status = "Unexpected failure at reading the chobby rapid package";
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unexpected error: {0}", ex);
                Status = "Unexpected error preparing chobby launch: " + ex.Message;
                return false;
            }
        }

        private static void ExtractDefaultConfigs(SpringPaths paths, PackageDownloader.Version ver)
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

        private dynamic ExtractEngineFromLua(PackageDownloader.Version ver)
        {
            var mi = ver.ReadFile(paths, "modinfo.lua");
            var lua = new Lua();
            var luaEnv = lua.CreateEnvironment();
            dynamic result = luaEnv.DoChunk(new StreamReader(mi), "dummy.lua");
            var engineVersion = result.engine;
            return engineVersion;
        }


        private void LaunchChobby(SpringPaths paths, string internalName, string engineVersion)
        {
            var process = new Process { StartInfo = { CreateNoWindow = true, UseShellExecute = false } };

            paths.SetDefaultEnvVars(process.StartInfo, engineVersion);

            process.StartInfo.FileName = paths.GetSpringExecutablePath(engineVersion);
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.GetSpringExecutablePath(engineVersion));
            process.StartInfo.Arguments = $"--menu \"{internalName}\"";

            process.Start();
        }
    }
}