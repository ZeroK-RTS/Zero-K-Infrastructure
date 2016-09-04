using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using AutoRegistrator;
using PlasmaDownloader;
using ZkData;
using ZkData.UnitSyncLib;

namespace ZeroKWeb
{
    public class AutoRegistrator
    {

        public SpringPaths Paths;
        public SpringScanner Scanner;
        public PlasmaDownloader.PlasmaDownloader Downloader;

        private string sitePath;
        public AutoRegistrator(string sitePath)
        {
            this.sitePath = sitePath;
        }

        public Thread RunMainAndMapSyncAsync()
        {
            var thread = new Thread(
                () =>
                {
                    rerun:
                    try
                    {
                        Main();
                        while (true)
                        {
                            Thread.Sleep(61 * 7 * 1000);
                            SynchronizeMapsFromSpringFiles();
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Autoregistrator failure: {0}", ex);
                        goto rerun;
                    }
                });
            thread.Start();
            return thread;
        }


        private string lastStableVersion;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public void Main()
        {

            Paths = new SpringPaths(Path.Combine(sitePath, "autoregistrator"), false);
            Scanner = new SpringScanner(Paths) { UseUnitSync = true };

            Scanner.LocalResourceAdded += (s, e) => Trace.TraceInformation("Autoregistrator ew resource found: {0}", e.Item.InternalName);
            Scanner.LocalResourceRemoved += (s, e) => Trace.TraceInformation("Autoregistrator Resource removed: {0}", e.Item.InternalName);

            SpringScanner.MapRegistered += (s, e) => Trace.TraceInformation("Autoregistrator Map registered: {0}", e.MapName);
            SpringScanner.ModRegistered += (s, e) => Trace.TraceInformation("Autoregistrator Mod registered: {0}", e.Data.Name);


            Downloader = new PlasmaDownloader.PlasmaDownloader(Scanner, Paths);
            Downloader.DownloadAdded += (s, e) => Trace.TraceInformation("Autoregistrator Download started: {0}", e.Data.Name);
            Downloader.GetResource(DownloadType.ENGINE, GlobalConst.DefaultEngineOverride)?.WaitHandle.WaitOne(); //for ZKL equivalent, see PlasmaShared/GlobalConst.cs
            Scanner.InitialScan();

            Downloader.PackageDownloader.SetMasterRefreshTimer(20);

            Downloader.PackagesChanged += Downloader_PackagesChanged;
            Downloader.PackageDownloader.LoadMasterAndVersions(false).Wait();
            Downloader.GetResource(DownloadType.MOD, "zk:stable")?.WaitHandle.WaitOne();
            Downloader.GetResource(DownloadType.MOD, "zk:test")?.WaitHandle.WaitOne();

            lastStableVersion = Downloader.PackageDownloader.GetByTag("zk:stable").InternalName;

            foreach (var ver in Downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByTag).Where(x => x.Key.StartsWith("spring-features")))
            {
                Downloader.GetResource(DownloadType.UNKNOWN, ver.Value.InternalName)?.WaitHandle.WaitOne();
            }

            Scanner.Start();

            SynchronizeMapsFromSpringFiles();

            while (Scanner.GetWorkCost() > 0) Thread.Sleep(1000);
        }

        private void SynchronizeMapsFromSpringFiles()
        {
            if (GlobalConst.Mode == ModeType.Live)
            {
                var fs = new WebFolderSyncer();
                fs.SynchronizeFolders("http://api.springfiles.com/files/maps/", Path.Combine(Paths.WritableDirectory, "maps"));
            }
        }

        static object Locker = new object();

        void Downloader_PackagesChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (var ver in Downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByTag.Keys))
                {
                    if (ver == "zk:stable" || ver == "zk:test")
                    {
                        Downloader.GetResource(DownloadType.MOD, ver)?.WaitHandle.WaitOne();
                    }
                }


                var waiting = false;
                do
                {
                    var downs = Downloader.Downloads.ToList().Where(x => x.IsComplete == null && !x.IsAborted).ToList();
                    if (downs.Any())
                    {
                        waiting = true;
                        var d = downs.First();
                        Trace.TraceInformation("Autoregistrator Waiting for: {0} - {1} {2}", d.Name, d.TotalProgress, d.TimeRemaining);
                    }
                    else if (Scanner.GetWorkCost() > 0)
                    {
                        waiting = true;
                        Trace.TraceInformation("Autoregistrator Waiting for scanner: {0}", Scanner.GetWorkCost());
                    }
                    else waiting = false;
                    if (waiting) Thread.Sleep(10000);
                } while (waiting);


                UpdateRapidTagsInDb();


                lock (Locker)
                {
                    foreach (var id in
                        new ZkDataContext(false).Missions.Where(x => !x.IsScriptMission && x.ModRapidTag != "" && !x.IsDeleted)
                            .Select(x => x.MissionID)
                            .ToList())
                    {
                        using (var db = new ZkDataContext(false))
                        {
                            var mis = db.Missions.Single(x => x.MissionID == id);
                            try
                            {
                                if (!string.IsNullOrEmpty(mis.ModRapidTag))
                                {
                                    var latestMod = Downloader.PackageDownloader.GetByTag(mis.ModRapidTag);
                                    if (latestMod != null && (mis.Mod != latestMod.InternalName || !mis.Resources.Any()))
                                    {
                                        mis.Mod = latestMod.InternalName;
                                        Trace.TraceInformation("Autoregistrator Updating mission {0} {1} to {2}", mis.MissionID, mis.Name, mis.Mod);
                                        var mu = new MissionUpdater();

                                        mis.Revision++;
                                        mu.UpdateMission(db, mis, Scanner);
                                        db.SaveChanges();
                                    }

                                }


                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Autoregistrator Failed to update mission {0}: {1}", mis.MissionID, ex);
                            }
                        }
                    }

                    try
                    {
                        var newName = Downloader.PackageDownloader.GetByTag("zk:stable").InternalName;
                        if (lastStableVersion != newName)
                        {
                            Trace.TraceInformation("Autoregistrator Generating steam stable package");
                            lastStableVersion = newName;
                            var pgen = new SteamDepotGenerator(sitePath,
                                Path.GetFullPath(Path.Combine(sitePath, "..", "steamworks", "tools", "ContentBuilder", "content")));
                            pgen.Generate();
                            pgen.RunBuild();
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Autoregistrator Error building steam package: {0}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Autoregistrator Error updating packages: {0}", ex);
            }
        }

        private void UpdateRapidTagsInDb()
        {
            using (var db = new ZkDataContext())
            {
                foreach (var ver in Downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByInternalName.Values))
                {
                    foreach (var toStrip in db.Resources.Where(x => x.RapidTag == ver.Name && x.InternalName != ver.InternalName))
                    {
                        toStrip.RapidTag = Downloader.PackageDownloader.GetByInternalName(toStrip.InternalName)?.Name;
                    }

                    foreach (var toSet in db.Resources.Where(x => x.RapidTag != ver.Name && x.InternalName == ver.InternalName))
                    {
                        toSet.RapidTag = ver.Name;
                    }
                }
                db.SaveChanges();
            }
        }
    }
}
