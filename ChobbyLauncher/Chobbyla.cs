using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.IronLua;
using PlasmaDownloader;
using ZkData;

namespace ChobbyLauncher
{
    public class Chobbyla
    {
        public string Status { get; private set; }
        public int? Progress { get; private set; }
        public Download Download { get; private set; }

        private SpringPaths paths;
        private string chobbyTag;

        public Chobbyla(string rootPath, string chobbyTagOverride, string engineOverride)
        {
            paths = new SpringPaths(rootPath, false);
            chobbyTag = chobbyTagOverride ?? "chobby:stable";
        }


        public async Task<bool> Run()
        {
            Status = "Checking for self-upgrade";

            var task = new Task<bool>(() => new SelfUpdater().CheckForUpdate());
            task.Start();
            await task;

            Status = "Checking for chobby update";
            

            var downloader = new PlasmaDownloader.PlasmaDownloader(new SpringScanner(paths) { WatchingEnabled = false, UseUnitSync = false }, paths);
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
                var mi = ver.ReadFile(paths, "modinfo.lua");
                var lua = new Lua();
                var luaEnv = lua.CreateEnvironment();
                dynamic result = luaEnv.DoChunk(new StreamReader(mi), "dummy.lua");
                var engineVersion = result.engine ?? "103.0.1-95-g20ebb8c";

                Status = "Downloading engine";
                Download = downloader.GetResource(DownloadType.ENGINE, engineVersion);
                var engDlTask = Download?.WaitHandle.AsTask(TimeSpan.FromMinutes(20));
                if (engDlTask != null) await engDlTask;
                if (Download?.IsComplete == false)
                {
                    Status = $"Download of engine {Download.Name} has failed";
                    return false;
                }

                Status = "Starting";
                LaunchChobby(paths, ver.InternalName, engineVersion);
                return true;
            }
            else
            {
                Status = "Unexpected failure at reading the chobby rapid package";
                return false;
            }
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
