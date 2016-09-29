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
        public UnitSyncer UnitSyncer;
        public PlasmaDownloader.PlasmaDownloader Downloader;

        public event EventHandler<string> NewZkStableRegistered = delegate (object sender, string s) { };

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



        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public void Main()
        {

            Paths = new SpringPaths(Path.Combine(sitePath, "autoregistrator"), false);

            Downloader = new PlasmaDownloader.PlasmaDownloader(null, Paths);
            Downloader.DownloadAdded += (s, e) => Trace.TraceInformation("Autoregistrator Download started: {0}", e.Data.Name);
            Downloader.GetResource(DownloadType.ENGINE, MiscVar.DefaultEngine)?.WaitHandle.WaitOne(); //for ZKL equivalent, see PlasmaShared/GlobalConst.cs

            UnitSyncer = new UnitSyncer(Paths, MiscVar.DefaultEngine);
            
            Downloader.PackageDownloader.SetMasterRefreshTimer(120);
            Downloader.PackagesChanged += Downloader_PackagesChanged;
            Downloader.PackageDownloader.LoadMasterAndVersions()?.Wait();
            Downloader.GetResource(DownloadType.MOD, "zk:stable")?.WaitHandle.WaitOne();
            Downloader.GetResource(DownloadType.MOD, "zk:test")?.WaitHandle.WaitOne();

            foreach (var ver in Downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByTag).Where(x => x.Key.StartsWith("spring-features")))
            {
                Downloader.GetResource(DownloadType.MOD, ver.Value.InternalName)?.WaitHandle.WaitOne();
            }
            
        }

        private void SynchronizeMapsFromSpringFiles()
        {
            if (GlobalConst.Mode == ModeType.Live)
            {
                var fs = new WebFolderSyncer();
                fs.SynchronizeFolders("http://api.springfiles.com/files/maps/", Path.Combine(Paths.WritableDirectory, "maps"));
                UnitSyncer.Scan();
            }
        }

        static object Locker = new object();

        void Downloader_PackagesChanged(object sender, EventArgs e)
        {
            try
            {
                Trace.TraceInformation("Autoregistrator packages changed");
                foreach (var ver in Downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByTag.Keys))
                {
                    if (ver == "zk:stable" || ver == "zk:test")
                    {
                        Trace.TraceInformation("Autoregistrator downloading {0}", ver);
                        Downloader.GetResource(DownloadType.MOD, ver)?.WaitHandle.WaitOne();
                    }
                }

                Trace.TraceInformation("Autoregistrator rescanning");
                UnitSyncer.Scan();

                Trace.TraceInformation("Autoregistrator scanning done");

                UpdateRapidTagsInDb();
                Trace.TraceInformation("Autoregistrator rapid tags updated");


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

                                        mu.UpdateMission(db, mis, UnitSyncer.Paths, UnitSyncer.Engine);
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

                    var newName = Downloader.PackageDownloader.GetByTag("zk:stable").InternalName;
                    if (MiscVar.LastRegisteredZkVersion != newName)
                    {
                        Trace.TraceInformation("Autoregistrator Generating steam stable package");
                        MiscVar.LastRegisteredZkVersion = newName;
                        try
                        {
                            var pgen = new SteamDepotGenerator(sitePath,
                                Path.GetFullPath(Path.Combine(sitePath, "..", "steamworks", "tools", "ContentBuilder", "content")));
                            pgen.Generate();
                            pgen.RunBuild();
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Autoregistrator Error building steam package: {0}", ex);
                        }
                        NewZkStableRegistered(this, newName);
                    }
                }

                Trace.TraceInformation("Autoregistrator all done");
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
                    var entry = db.Resources.FirstOrDefault(x => x.InternalName == ver.InternalName && x.RapidTag != ver.Name);
                    if (entry != null) entry.RapidTag = ver.Name;
                }
                db.SaveChanges();
            }
        }
    }
}
