using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using AutoRegistrator;
using PlasmaDownloader;
using ZkData;

namespace ZeroKWeb
{
    public class AutoRegistrator
    {
        private static object Locker = new object();
        public PlasmaDownloader.PlasmaDownloader Downloader;

        private DateTime lastSpringFilesUpdate = new DateTime();

        public SpringPaths Paths;

        private string sitePath;
        public UnitSyncer UnitSyncer;

        public AutoRegistrator(string sitePath)
        {
            this.sitePath = sitePath;
        }


        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        public void Main()
        {
            Paths = new SpringPaths(Path.Combine(sitePath, "autoregistrator"), false);

            
            Downloader = new PlasmaDownloader.PlasmaDownloader(null, Paths);
            Downloader.DownloadAdded += (s, e) => Trace.TraceInformation("Autoregistrator Download started: {0}", e.Data.Name);
            Downloader.GetResource(DownloadType.ENGINE, MiscVar.DefaultEngine)?.WaitHandle.WaitOne();
            //for ZKL equivalent, see PlasmaShared/GlobalConst.cs

            UnitSyncer = new UnitSyncer(Paths, MiscVar.DefaultEngine);

            Downloader.PackageDownloader.DoMasterRefresh();
            Downloader.GetResource(DownloadType.RAPID, "zk:stable")?.WaitHandle.WaitOne();
            Downloader.GetResource(DownloadType.RAPID, "zk:test")?.WaitHandle.WaitOne();

            foreach (
                var ver in Downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByTag).Where(x => x.Key.StartsWith("spring-features"))) Downloader.GetResource(DownloadType.RAPID, ver.Value.InternalName)?.WaitHandle.WaitOne();

            OnRapidChanged();
        }

        public event EventHandler<string> NewZkStableRegistered = delegate (object sender, string s) { };

        public Thread RunMainAndMapSyncAsync()
        {
            var thread = new Thread(() =>
            {
                rerun:
                try
                {
                    Main();
                    while (true)
                    {
                        Thread.Sleep(61 * 1000);
                        if (Downloader.PackageDownloader.DoMasterRefresh()) OnRapidChanged();
                        if (DateTime.UtcNow.Subtract(lastSpringFilesUpdate).TotalMinutes > 61)
                        {
                            lastSpringFilesUpdate = DateTime.UtcNow;
                            SynchronizeMapsFromSpringFiles();
                        }
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

        private void OnRapidChanged()
        {
            try
            {
                Trace.TraceInformation("Autoregistrator packages changed");
                foreach (var ver in Downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByTag.Keys))
                    if ((ver == "zk:stable") || (ver == "zk:test"))
                    {
                        Trace.TraceInformation("Autoregistrator downloading {0}", ver);
                        Downloader.GetResource(DownloadType.RAPID, ver)?.WaitHandle.WaitOne();
                    }

                Trace.TraceInformation("Autoregistrator rescanning");
                UnitSyncer.Scan();

                Trace.TraceInformation("Autoregistrator scanning done");

                UpdateRapidTagsInDb();
                Trace.TraceInformation("Autoregistrator rapid tags updated");

                lock (Locker)
                {
                    // UpdateMissions();

                    var newName = Downloader.PackageDownloader.GetByTag("zk:stable").InternalName;
                    if (MiscVar.LastRegisteredZkVersion != newName)
                    {
                        MiscVar.LastRegisteredZkVersion = newName;
                        if (GlobalConst.Mode == ModeType.Live)
                        {
                            Trace.TraceInformation("Autoregistrator Generating steam stable package");
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

        private void UpdateMissions()
        {
            foreach (var id in
                new ZkDataContext(false).Missions.Where(x => !x.IsScriptMission && (x.ModRapidTag != "") && !x.IsDeleted).Select(x => x.MissionID).ToList())
                using (var db = new ZkDataContext(false))
                {
                    var mis = db.Missions.Single(x => x.MissionID == id);
                    try
                    {
                        if (!string.IsNullOrEmpty(mis.ModRapidTag))
                        {
                            var latestMod = Downloader.PackageDownloader.GetByTag(mis.ModRapidTag);
                            if ((latestMod != null) && ((mis.Mod != latestMod.InternalName) || !mis.Resources.Any()))
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

        private void SynchronizeMapsFromSpringFiles()
        {
            if (GlobalConst.Mode == ModeType.Live)
            {
                var fs = new WebFolderSyncer();
                fs.SynchronizeFolders("http://api.springfiles.com/files/maps/", Path.Combine(Paths.WritableDirectory, "maps"));
                UnitSyncer.Scan();
            }
        }

        private void UpdateRapidTagsInDb()
        {
            using (var db = new ZkDataContext())
            {
                foreach (var ver in Downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByInternalName.Values))
                {
                    var entry = db.Resources.FirstOrDefault(x => (x.InternalName == ver.InternalName) && (x.RapidTag != ver.Name));
                    if (entry != null) entry.RapidTag = ver.Name;
                }
                db.SaveChanges();
            }
        }
    }
}