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
            Paths = new SpringPaths(Path.Combine(sitePath, "autoregistrator"), false, false);

            // delete all packages to speed up startup
            foreach (var f in Directory.GetFiles(Path.Combine(Paths.WritableDirectory, "packages")))
            {
                File.Delete(f);
            }


            Downloader = new PlasmaDownloader.PlasmaDownloader(null, Paths);
            Downloader.DownloadAdded += (s, e) => Trace.TraceInformation("Autoregistrator Download started: {0}", e.Data.Name);
            Downloader.GetResource(DownloadType.ENGINE, GlobalConst.UnitSyncEngine)?.WaitHandle.WaitOne();
            //for ZKL equivalent, see PlasmaShared/GlobalConst.cs

            UnitSyncer = new UnitSyncer(Paths, GlobalConst.UnitSyncEngine);

            Downloader.PackageDownloader.DoMasterRefresh();
            //LoadAllSpringFeatures();

            OnRapidChanged();
        }

        private void LoadAllSpringFeatures()
        {
            foreach (var ver in Downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByTag).Where(x => x.Key.StartsWith("spring-features")))
                Downloader.GetResource(DownloadType.RAPID, ver.Value.InternalName)?.WaitHandle.WaitOne();
        }

        public event Action<string, string> NewZkReleaseRegistered = (zk, chobby) => { };


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

                        /*
                        if (DateTime.UtcNow.Subtract(lastSpringFilesUpdate).TotalMinutes > 61)
                        {
                            lastSpringFilesUpdate = DateTime.UtcNow;
                            SynchronizeMapsFromSpringFiles();
                        }*/
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
                lock (Locker)
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

                    var newName = Downloader.PackageDownloader.GetByTag(GlobalConst.DefaultZkTag).InternalName;
                    var newChobbyName = Downloader.PackageDownloader.GetByTag(GlobalConst.DefaultChobbyTag).InternalName;
                    if (MiscVar.LastRegisteredZkVersion != newName || MiscVar.LastRegisteredChobbyVersion != newChobbyName)
                    {
                        NewZkReleaseRegistered(newName, newChobbyName);
                        MiscVar.LastRegisteredZkVersion = newName;
                        MiscVar.LastRegisteredChobbyVersion = newChobbyName;
                    }
                }

                Trace.TraceInformation("Autoregistrator all done");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Autoregistrator Error updating packages: {0}", ex);
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