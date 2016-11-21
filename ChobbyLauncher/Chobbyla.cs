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
using Newtonsoft.Json;
using PlasmaDownloader;
using PlasmaDownloader.Packages;
using PlasmaShared;
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

        public SpringPaths paths;
        private string internalName;
        private int loopbackPort;
        public PlasmaDownloader.PlasmaDownloader downloader;
        public Download Download { get; private set; }
        public string Status { get; private set; }
        public Process process { get; private set; }


        public Chobbyla(string rootPath, string chobbyTagOverride, string engineOverride)
        {
            paths = new SpringPaths(rootPath, false);
            chobbyTag = chobbyTagOverride ?? (GlobalConst.Mode == ModeType.Live ? "zkmenu:stable" : "zkmenu:test");
            isDev = chobbyTag == "dev" || chobbyTag == "chobby:dev" || chobbyTag =="zkmenu:dev";
            engine = engineOverride;
            downloader = new PlasmaDownloader.PlasmaDownloader(null, paths);
        }

        

        public Task Run()
        {
            return LaunchChobby(paths, internalName, engine, loopbackPort);
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
                            if (Environment.OSVersion.Platform == PlatformID.Unix)
                            {
                                Process.Start(Application.ExecutablePath);
                                Environment.Exit(0);
                            } else Application.Restart();
                        };
                        var task = new Task<bool>(() => selfUpdater.CheckForUpdate());
                        task.Start();
                        await task;
                    }
                    
                    if (!await DownloadFile("Checking for chobby update", DownloadType.RAPID, chobbyTag)) return false;
                    if (!await DownloadFile("Checking for game update", DownloadType.RAPID, "zk:stable")) return false;
                    
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

                if (!await DownloadFile("Downloading engine", DownloadType.ENGINE, engine)) return false;

                if (!await UpdateMissions())
                {
                    Trace.TraceWarning("Mission update has failed");
                    Status = "Error updating missions";
                }


                if (!isDev)
                {
                    Status = "Reseting configs";
                    ConfigVersions.ResetConfigs(paths, ver);

                    Status = "Extracting default configs";
                    ExtractDefaultConfigs(paths, ver);
                }
                
                Status = "Starting";

                var chobyl = new ChobbylaLocalListener(this);
                loopbackPort = chobyl.StartListening();
                
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unexpected error: {0}", ex);
                Status = "Unexpected error preparing chobby launch: " + ex.Message;
                return false;
            }
        }



        private async Task<bool> DownloadFile(string desc, DownloadType type, string name)
        {
            Status = desc;
            Download = downloader.GetResource(type,  name);
            var dlTask = Download?.WaitHandle.AsTask(TimeSpan.FromMinutes(30));
            if (dlTask != null) await dlTask;
            if (Download?.IsComplete == false)
            {
                Status = $"Download of {Download.Name} has failed";
                return false;
            }
            return true;
        }

        private async Task<bool> DownloadUrl(string desc, string url, string filePathTarget)
        {
            Status = desc;
            var wfd = new WebFileDownload(url, filePathTarget, paths.Cache);
            wfd.Start();
            Download = wfd;
            var dlTask = Download?.WaitHandle.AsTask(TimeSpan.FromMinutes(30));
            if (dlTask != null) await dlTask;
            if (Download?.IsComplete == false)
            {
                Status = $"Download of {Download.Name} has failed";
                return false;
            }
            return true;
        }



        private async Task<bool> UpdateMissions()
        {
            Status = "Downloading missions";
            var missions = GlobalConst.GetContentService().GetDefaultMissions();

            var missionsFolder = Path.Combine(paths.WritableDirectory, "missions");
            if (!Directory.Exists(missionsFolder)) Directory.CreateDirectory(missionsFolder);
            var missionFile = Path.Combine(missionsFolder, "missions.json");

            List<ClientMissionInfo> existing = null;
            if (File.Exists(missionFile))
            {
                try
                {
                    existing = JsonConvert.DeserializeObject<List<ClientMissionInfo>>(File.ReadAllText(missionFile));
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Error reading mission file {0} : {1}", missionFile, ex);
                }
            }
            existing = existing ?? new List<ClientMissionInfo>();

            var toDownload =
                missions.Where(m => !existing.Any(x => x.MissionID == m.MissionID && x.Revision == m.Revision && x.DownloadHandle == m.DownloadHandle))
                    .ToList();

            // download mission files
            foreach (var m in toDownload)
            {
                if (!m.IsScriptMission) if (!await DownloadFile("Downloading mission " + m.DisplayName, DownloadType.MISSION, m.DownloadHandle)) return false;
                if (!await DownloadUrl("Downloading image", m.ImageUrl, Path.Combine(missionsFolder, $"{m.MissionID}.png"))) return false;
            }
            

            File.WriteAllText(missionFile, JsonConvert.SerializeObject(missions));

            return true;
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
            process = new Process { StartInfo = { CreateNoWindow = true, UseShellExecute = false } };

            paths.SetDefaultEnvVars(process.StartInfo, engineVersion);
            var widgetFolder = Path.Combine(paths.WritableDirectory);//, "LuaMenu", "Widgets");
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