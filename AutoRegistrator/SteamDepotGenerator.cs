using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public SteamDepotGenerator(string siteBase, string targetFolder) {
            this.targetFolder = targetFolder;
            this.siteBase = siteBase;
        }

        public void Generate() {
            
            var paths = new SpringPaths(null, targetFolder, false);
            try
            {
                //Directory.Delete(Path.Combine(paths.WritableDirectory, "pool"), true);
                Directory.Delete(Path.Combine(paths.WritableDirectory, "packages"), true);
            } catch { }
            

            paths.MakeFolders();
            var downloader = new PlasmaDownloader.PlasmaDownloader(new Config(), null, paths);
            downloader.GetAndSwitchEngine(GlobalConst.DefaultEngineOverride)?.WaitHandle.WaitOne(); //for ZKL equivalent, see PlasmaShared/GlobalConst.cs
            downloader.PackageDownloader.LoadMasterAndVersions(false).Wait();
            downloader.GetResource(DownloadType.MOD, "zk:stable")?.WaitHandle.WaitOne();
            downloader.GetResource(DownloadType.MOD, "zk:test")?.WaitHandle.WaitOne();

            CopyResources(siteBase, paths, GetResourceList(downloader.PackageDownloader.GetByTag("zk:stable").InternalName, downloader.PackageDownloader.GetByTag("zk:test").InternalName), downloader);

            var zklSource = Path.Combine(siteBase, "lobby", "Zero-K.exe");
            if (File.Exists(zklSource)) File.Copy(zklSource, Path.Combine(targetFolder, "Zero-K.exe"), true);
            else
            {
                new WebClient().DownloadFile(GlobalConst.SelfUpdaterBaseUrl + "/" + "Zero-K.exe", Path.Combine(targetFolder, "Zero-K.exe"));
            }

            using (var scanner = new SpringScanner(paths) {UseUnitSync = false})
            {
                scanner.InitialScan();
                scanner.Start();

                while (scanner.GetWorkCost() > 0)
                {
                    Trace.TraceInformation("Waiting for scanner to complete");
                    Thread.Sleep(5000);
                }
            }
        }


        private static void CopyResources(string siteBase, SpringPaths paths, List<Resource> resources, PlasmaDownloader.PlasmaDownloader downloader) {
            var destMaps = Path.Combine(paths.WritableDirectory, "maps");
            var sourceMaps = Path.Combine(siteBase, "autoregistrator", "maps");
            var sourceMetadata = Path.Combine(siteBase, "Resources");
            var targetMetadata = Path.Combine(paths.Cache, "Resources");
            if (!Directory.Exists(targetMetadata)) Directory.CreateDirectory(targetMetadata);

            foreach (var res in resources)
            {
                Trace.TraceInformation("Copying {0}", res.InternalName);
                if (res.TypeID == ResourceType.Map)
                {
                    var fileName = res.ResourceContentFiles.ToList().Where(x=>File.Exists(Path.Combine(sourceMaps, x.FileName))).OrderByDescending(x => x.LinkCount).FirstOrDefault()?.FileName; // get registered file names

                    fileName = fileName?? res.ResourceContentFiles
                        .SelectMany(x => x.Links.Split('\n'))
                        .Where(x => x != null).Select(x=> x.Substring(x.LastIndexOf('/')+1, x.Length - x.LastIndexOf('/') - 1)).FirstOrDefault(x => !string.IsNullOrEmpty(x) && File.Exists(Path.Combine(sourceMaps,x))); // get filenames from url

                    if (fileName == null) Trace.TraceError("Cannot find map file: {0}", res.InternalName);


                    if (!File.Exists(Path.Combine(destMaps, fileName))) File.Copy(Path.Combine(sourceMaps, fileName), Path.Combine(destMaps, fileName));
                } else if (res.MissionID != null) File.WriteAllBytes(Path.Combine(paths.WritableDirectory, "games", res.Mission.SanitizedFileName), res.Mission.Mutator);
                else downloader.GetResource(DownloadType.UNKNOWN, res.InternalName)?.WaitHandle.WaitOne();

                foreach (var metaName in new[] { res.MinimapName, res.HeightmapName, res.MetalmapName, res.MetadataName, res.ThumbnailName })
                {
                    Trace.TraceInformation("Copying resource: {0}", metaName);
                    var src = Path.Combine(sourceMetadata, metaName);
                    var dst = Path.Combine(targetMetadata, metaName);
                    if (!File.Exists(dst) && File.Exists(src))
                    {
                        File.Copy(src, dst);
                    }
                }
            }
        }


        private static List<Resource> GetResourceList(params string[] extraNames) {
            

            var db = new ZkDataContext();
            var resources = db.Resources.Where(x => extraNames.Contains(x.InternalName) || (x.TypeID == ResourceType.Map && x.FeaturedOrder != null) || (x.MissionID != null && !x.Mission.IsDeleted && x.Mission.FeaturedOrder !=null)).ToList();
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

        public void RunBuild() {
            var pi = new ProcessStartInfo(Path.Combine(targetFolder,"..","builder","steamcmd.exe"), string.Format(@"+login zkbuild {0} +run_app_build_http ..\scripts\app_zk_stable.vdf +quit", new Secrets().GetSteamBuildPassword()));
            pi.UseShellExecute = false;
            pi.WindowStyle = ProcessWindowStyle.Hidden;
            var runp = Process.Start(pi);
            runp.WaitForExit();
        }
    }
}
