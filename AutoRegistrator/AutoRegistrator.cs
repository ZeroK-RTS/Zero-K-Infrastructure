using System;
using System.Collections.Generic;
using System.Data.Entity;
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
            Paths = new SpringPaths(Path.Combine(sitePath, "autoregistrator"), false, Environment.Is64BitProcess);

            // delete all maps and packages to speed up startup
            DeleteAllPackages();
            DeleteAllMaps();



            Downloader = new PlasmaDownloader.PlasmaDownloader(null, Paths);
            Downloader.DownloadAdded += (s, e) => Trace.TraceInformation("Autoregistrator Download started: {0}", e.Data.Name);
            Downloader.GetResource(DownloadType.ENGINE, GlobalConst.UnitSyncEngine)?.WaitHandle.WaitOne();
            //for ZKL equivalent, see PlasmaShared/GlobalConst.cs

            UnitSyncer = new UnitSyncer(Paths, GlobalConst.UnitSyncEngine);

            Downloader.PackageDownloader.DoMasterRefresh();
            //LoadAllSpringFeatures();

            OnRapidChanged();
        }

        private void DeleteAllPackages()
        {
            foreach (var f in Directory.GetFiles(Path.Combine(Paths.WritableDirectory, "packages")))
            {
                File.Delete(f);
            }
        }

        private void DeleteAllMaps()
        {
            foreach (var f in Directory.GetFiles(Path.Combine(Paths.WritableDirectory, "maps")))
            {
                File.Delete(f);
            }
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
            if (GlobalConst.Mode != ModeType.Live) return;

            HashSet<string> processedFiles, triedFiles;
            using (var db = new ZkDataContext())
            {
                processedFiles = new HashSet<string>(
                    db.ResourceContentFiles.AsNoTracking()
                        .Where(x => x.Resource.TypeID == ResourceType.Map)
                        .Select(x => x.FileName.ToLower()));
                triedFiles = new HashSet<string>(
                    db.SpringFilesUnitsyncAttempts.AsNoTracking().Select(x => x.FileName.ToLower()));
            }

            var webSyncer = new WebFolderSyncer();
            var autoregMaps = Path.Combine(Paths.WritableDirectory, "maps");
            var contentMaps = Path.Combine(sitePath, "content", "maps");
            if (!Directory.Exists(contentMaps)) Directory.CreateDirectory(contentMaps);

            foreach (var file in webSyncer.GetFileList())
            {
                var fileLc = file.ToLower();
                var alreadyRegistered = processedFiles.Contains(fileLc);
                var alreadyAttempted = triedFiles.Contains(fileLc);
                var contentFile = Path.Combine(contentMaps, file);
                var hasLocal = File.Exists(contentFile);

                // skip cases:
                //  - registered AND local copy present → nothing to do
                //  - previously attempted but never made it into DB → known failure, don't retry
                if (alreadyRegistered && hasLocal) continue;
                if (alreadyAttempted && !alreadyRegistered) continue;

                if (!webSyncer.DownloadFile(autoregMaps, file)) continue;
                var srcFile = Path.Combine(autoregMaps, file);

                var registered = alreadyRegistered;
                if (!alreadyRegistered)
                {
                    var results = UnitSyncer.Scan();
                    var ourResult = results?.FirstOrDefault(r => r.ResourceInfo?.ArchiveName == file);
                    registered = ourResult != null && ourResult.Status != UnitSyncer.ResourceFileStatus.RegistrationError;

                    using (var db = new ZkDataContext())
                    {
                        db.SpringFilesUnitsyncAttempts.Add(new SpringFilesUnitsyncAttempt() { FileName = file });
                        db.SaveChanges();
                    }
                    triedFiles.Add(fileLc);
                }

                if (registered && !hasLocal)
                {
                    try { File.Copy(srcFile, contentFile); }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Copy {0} to content/maps failed: {1}", file, ex.Message);
                    }
                }

                try { File.Delete(srcFile); } catch { }
            }
        }

        private void UpdateRapidTagsInDb()
        {
            using (var db = new ZkDataContext())
            {
                var internalNameToTag = Downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByInternalName.Values)
                    .ToLookup(x => x.InternalName, x => x);

                foreach (var entry in db.Resources.Where(x => x.TypeID != ResourceType.Map).ToList())
                {
                    var tag = internalNameToTag[entry.InternalName].Select(x => x.Name).FirstOrDefault();
                    if (entry.RapidTag != tag && tag != null)
                    {
                        entry.RapidTag = tag;
                        db.SaveChanges();
                    }
                }
            }
        }
    }
}