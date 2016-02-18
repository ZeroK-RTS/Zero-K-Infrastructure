using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PlasmaDownloader;
using ZeroKWeb;
using ZkData;

namespace AutoRegistrator
{
    public class SteamDepotGenerator
    {
        public void Generate(string siteBase, string targetFolder, ModeType mode) {
            
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
            downloader.GetResource(DownloadType.MOD, mode == ModeType.Test ? "zk:test" : "zk:stable")?.WaitHandle.WaitOne();
            CopyResources(siteBase, paths, GetResourceList(), downloader);

            var zklSource = Path.Combine(siteBase, "lobby", "Zero-K.exe");
            if (File.Exists(zklSource)) File.Copy(zklSource, Path.Combine(targetFolder, "Zero-K.exe"), true);
            else
            {
                new WebClient().DownloadFile(GlobalConst.SelfUpdaterBaseUrl + "/" + "Zero-K.exe", Path.Combine(targetFolder, "Zero-K.exe"));
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
                    var fileName = res.ResourceContentFiles.OrderByDescending(x => x.LinkCount).First().FileName;
                    if (!File.Exists(Path.Combine(destMaps, fileName))) File.Copy(Path.Combine(sourceMaps, fileName), Path.Combine(destMaps, fileName));
                } else if (res.MissionID != null) File.WriteAllBytes(Path.Combine(paths.WritableDirectory, "games", res.Mission.SanitizedFileName), res.Mission.Mutator);
                else downloader.GetResource(DownloadType.UNKNOWN, res.InternalName)?.WaitHandle.WaitOne();

                foreach (var metaName in new[] { res.MinimapName, res.HeightmapName, res.MetalmapName, res.MetadataName, res.ThumbnailName })
                {
                    var src = Path.Combine(sourceMetadata, metaName);
                    var dst = Path.Combine(targetMetadata, metaName);
                    if (!File.Exists(dst)) File.Copy(src, dst);
                }
            }
        }


        private static List<Resource> GetResourceList() {
            var db = new ZkDataContext();
            var resources = db.Resources.Where(x => (x.TypeID == ResourceType.Map && x.FeaturedOrder != null) || (x.MissionID != null && !x.Mission.IsDeleted && x.Mission.FeaturedOrder !=null)).ToList();
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
    }
}
