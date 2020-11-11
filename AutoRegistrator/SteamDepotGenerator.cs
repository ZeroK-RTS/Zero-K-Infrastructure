using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PlasmaDownloader;
using ZeroKWeb;
using ZkData;

namespace AutoRegistrator
{
    public class SteamDepotGenerator
    {
        string targetFolder;
        private string siteBase;
        public SteamDepotGenerator(string siteBase, string targetFolder)
        {
            this.targetFolder = targetFolder;
            this.siteBase = siteBase;
        }

        private object locker = new object();

        public void RunAll()
        {
            if (GlobalConst.Mode != ModeType.Local)
            {
                lock (locker)
                {
                    Trace.TraceInformation("SteamDepot gnerating steam package");
                    Generate();
                    RunBuild();
                    PublishBuild();
                }
            }
            else Trace.TraceWarning("SteamDepot generating steam package SKIPPED in debug mode");
        }

        public class DummyProgress : IChobbylaProgress
        {
            public Download Download { get; set; }
            public string Status { get; set; }
        }
        private void Generate()
        {
            Utils.CheckPath(targetFolder);
            try
            {
                Directory.Delete(Path.Combine(targetFolder, "maps"), true);
                Directory.Delete(Path.Combine(targetFolder, "packages"), true);
                Directory.Delete(Path.Combine(targetFolder, "engine"), true);
                Directory.Delete(Path.Combine(targetFolder, "games"), true);
                Directory.Delete(Path.Combine(targetFolder, "rapid"), true);
                Directory.Delete(Path.Combine(targetFolder, "cache"), true);
                Directory.Delete(Path.Combine(targetFolder, "temp"), true);
                File.Delete(Path.Combine(targetFolder, "missions", "missions.json"));
            }
            catch { }

            var paths = new SpringPaths(targetFolder, false, false);
            var downloader = new PlasmaDownloader.PlasmaDownloader(null, paths);
            var prog = new DummyProgress();


            foreach (var plat in Enum.GetValues(typeof(SpringPaths.PlatformType)).Cast<SpringPaths.PlatformType>())
            {
                var sp = new SpringPaths(targetFolder, false, false, plat);
                var dn = new PlasmaDownloader.PlasmaDownloader(null, sp);

                if (!dn.DownloadFile(DownloadType.ENGINE, MiscVar.DefaultEngine, prog).Result) throw new ApplicationException("SteamDepot engine download failed: " + prog.Status);
            }

            downloader.PackageDownloader.DoMasterRefresh();

            var chobbyName = downloader.PackageDownloader.GetByTag(GlobalConst.DefaultChobbyTag).InternalName;

            downloader.RapidHandling = RapidHandling.SdzNameTagForceDownload;

            if (!downloader.DownloadFile(DownloadType.RAPID, chobbyName, prog).Result) throw new ApplicationException("SteamDepot chobby download failed: " + prog.Status);
            if (!downloader.DownloadFile(DownloadType.RAPID, GlobalConst.DefaultZkTag, prog).Result) throw new ApplicationException("SteamDepot zk download failed: " + prog.Status);

            downloader.RapidHandling = RapidHandling.DefaultSdp;


            var campaignMaps = RegistratorRes.campaignMaps.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
                .ToArray();
            CopyResources(siteBase, paths, GetMapList(campaignMaps), downloader);

            // don't update missions on test ZK
            if (GlobalConst.Mode == ModeType.Live)
            {
                if (!downloader.UpdateMissions(prog).Result) throw new ApplicationException("SteamDepot Error updating missions! " + prog.Status);
            }
            if (!downloader.UpdatePublicCommunityInfo(prog)) throw new ApplicationException("SteamDepot Error updating public community info! " + prog.Status);


            CopyLobbyProgram();
            CopyExtraImages();

            downloader.PackageDownloader.DoMasterRefresh();
           

            File.WriteAllText(Path.Combine(paths.WritableDirectory, "steam_chobby.txt"), chobbyName);
            File.WriteAllText(Path.Combine(paths.WritableDirectory, "steam_engine.txt"), MiscVar.DefaultEngine);
            File.WriteAllText(Path.Combine(paths.WritableDirectory, "steam_deletesdp.txt"), "1");
        }

        private void CopyLobbyProgram()
        {
            var zklSource = Path.Combine(siteBase, "lobby", "Zero-K.exe");
            if (File.Exists(zklSource)) File.Copy(zklSource, Path.Combine(targetFolder, "Zero-K.exe"), true);
            else new WebClient().DownloadFile(GlobalConst.SelfUpdaterBaseUrl + "/" + "Zero-K.exe", Path.Combine(targetFolder, "Zero-K.exe"));
        }

        private void CopyExtraImages()
        {
            var db = new ZkDataContext();
            var configs = Path.Combine(targetFolder, "LuaUI", "Configs");
            Utils.CheckPath(configs);

            var tpath = Path.Combine(configs, "Avatars");
            Utils.CheckPath(tpath);
            Trace.TraceInformation("Copying avatars");
            var spath = Path.Combine(siteBase, "img", "Avatars");
            Utils.CheckPath(spath);
            foreach (var file in Directory.GetFiles(spath)) File.Copy(file, Path.Combine(tpath, Path.GetFileName(file)), true);

            Trace.TraceInformation("Copying clan icons");
            tpath = Path.Combine(configs, "Clans");
            Utils.CheckPath(tpath);
            spath = Path.Combine(siteBase, "img", "clans");
            foreach (var clan in db.Clans.Where(x => !x.IsDeleted))
            {
                var fileName = $"{clan.Shortcut}.png";
                var src = Path.Combine(spath, fileName);
                if (File.Exists(src)) File.Copy(src, Path.Combine(tpath, fileName), true);
            }

            Trace.TraceInformation("Copying faction icons");
            tpath = Path.Combine(configs, "Factions");
            Utils.CheckPath(tpath);
            spath = Path.Combine(siteBase, "img", "factions");

            foreach (var fac in db.Factions.Where(x => !x.IsDeleted))
            {
                var fileName = $"{fac.Shortcut}.png";
                var src = Path.Combine(spath, fileName);
                if (File.Exists(src)) File.Copy(src, Path.Combine(tpath, fileName), true);
            }


            spath = Path.Combine(siteBase, "Resources");
            tpath = Path.Combine(targetFolder, "LuaMenu", "configs", "gameConfig", "zk");

            Utils.CheckPath(Path.Combine(tpath, "minimapThumbnail"));
            Utils.CheckPath(Path.Combine(tpath, "minimapOverride"));

            foreach (var map in db.Resources.Where(x => x.TypeID == ResourceType.Map && x.MapSupportLevel >= MapSupportLevel.Supported))
            {
                var sourceFile = Path.Combine(spath, map.MinimapName);
                if (File.Exists(sourceFile))
                {
                    var name = map.InternalName.EscapePath();
                    var thumbnailTarget = Path.Combine(tpath, "minimapThumbnail", $"{name}.png");
                    if (!File.Exists(thumbnailTarget))
                    {
                        using (var img = Image.FromFile(sourceFile))
                        {
                            using (var resized = Utils.GetResized(img, 64,64))
                            {
                                resized.Save(thumbnailTarget);
                            }
                        }
                    }

                    var minimapTarget = Path.Combine(tpath, "minimapOverride", $"{name}.jpg");
                    if (!File.Exists(minimapTarget)) File.Copy(sourceFile, minimapTarget);
                }
            }
        }


        private static void CopyResources(string siteBase, SpringPaths paths, List<Resource> resources, PlasmaDownloader.PlasmaDownloader downloader)
        {
            var destMaps = Path.Combine(paths.WritableDirectory, "maps");
            var sourceMaps = Path.Combine(siteBase, "autoregistrator", "maps");

            foreach (var res in resources)
            {
                Trace.TraceInformation("Copying {0}", res.InternalName);
                if (res.TypeID == ResourceType.Map)
                {
                    var fileName = res.ResourceContentFiles.ToList().Where(x => File.Exists(Path.Combine(sourceMaps, x.FileName))).OrderByDescending(x => x.LinkCount).FirstOrDefault()?.FileName; // get registered file names

                    fileName = fileName ?? res.ResourceContentFiles
                        .Where(x => x.Links != null).SelectMany(x => x.Links.Split('\n'))
                        .Where(x => x != null).Select(x => x.Substring(x.LastIndexOf('/') + 1, x.Length - x.LastIndexOf('/') - 1)).FirstOrDefault(x => !string.IsNullOrEmpty(x) && File.Exists(Path.Combine(sourceMaps, x))); // get filenames from url

                    if (fileName == null)
                    {
                        var prog = new DummyProgress();
                        if (!downloader.DownloadFile(DownloadType.MAP, res.InternalName, prog).Result) Trace.TraceError("Cannot find map file: {0}", res.InternalName);
                        continue;
                    }


                    if (!File.Exists(Path.Combine(destMaps, fileName))) File.Copy(Path.Combine(sourceMaps, fileName), Path.Combine(destMaps, fileName));
                }
                else if (res.MissionID != null) File.WriteAllBytes(Path.Combine(paths.WritableDirectory, "games", res.Mission.SanitizedFileName), res.Mission.Mutator);
                else downloader.GetResource(DownloadType.RAPID, res.InternalName)?.WaitHandle.WaitOne();
            }
        }


        private static List<Resource> GetMapList(params string[] extraNames)
        {
            var db = new ZkDataContext();
            DateTime limit = DateTime.Now.AddMonths(-2);
            
            var top100 = db.SpringBattles.Where(x => x.StartTime >= limit).Where(x=>x.ResourceByMapResourceID.MapSupportLevel >= MapSupportLevel.Supported).GroupBy(x => x.ResourceByMapResourceID).OrderByDescending(x => x.Sum(y => y.Duration *  y.SpringBattlePlayers.Count())).Take(50).Select(x=>x.Key.ResourceID).Take(50).ToList();

            //var pwMaps = db.Galaxies.Where(x => x.IsDefault).SelectMany(x => x.Planets).Where(x => x.MapResourceID != null).Select(x => x.MapResourceID.Value).ToList();

            //top100.AddRange(pwMaps);
            top100 = top100.Distinct().ToList();

            var resources = db.Resources.Where(x => top100.Contains(x.ResourceID) || extraNames.Contains(x.InternalName) || (x.TypeID == ResourceType.Map && x.MapSupportLevel >= MapSupportLevel.MatchMaker)).ToList();
            foreach (var res in resources.ToList())
            {
                foreach (var requestedDependency in res.ResourceDependencies.Select(x => x.NeedsInternalName))
                {
                    if (!resources.Any(y => y.InternalName == requestedDependency))
                    {
                        var dependency = db.Resources.FirstOrDefault(x => x.InternalName == requestedDependency);
                        if (dependency != null) resources.Add(dependency);
                    }
                }
            }
            return resources;
        }

        private void RunBuild()
        {
            Trace.TraceInformation("Starting SteamDepot build");
            var pi = new ProcessStartInfo(Path.Combine(targetFolder, "..", "builder", "steamcmd.exe"), string.Format(@"+login zkbuild {0} +run_app_build_http ..\scripts\{1}.vdf +quit", new Secrets().GetSteamBuildPassword(), GlobalConst.Mode == ModeType.Live ? "app_zk_stable" : "app_zk_test"));
            pi.UseShellExecute = false;
            pi.WindowStyle = ProcessWindowStyle.Hidden;
            var runp = Process.Start(pi);
            runp.WaitForExit();
            Trace.TraceInformation("SteamDepot build completed!");

            // cleanup output folder to save disk space
            var outputFolder = Path.Combine(targetFolder, "..", "output");
            foreach (var file in Directory.GetFiles(outputFolder))
            {
                if (!file.EndsWith(".log")) File.Delete(file);
            }
        }

        private void PublishBuild()
        {
            try
            {
                var steamWebApi = new SteamWebApi();
                var build = steamWebApi.GetAppBuilds().First(x => x.Description.ToLower().Contains(GlobalConst.Mode == ModeType.Live ? "stable" : "test"));
                steamWebApi.SetAppBuildLive(build.BuildID, GlobalConst.Mode == ModeType.Live ? "public" : "test");
                Trace.TraceInformation("SteamDepot build {0} set live", build.BuildID);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("SteamDepot error publishing steam branch: {0}", ex);
            }
        }
    }
}
