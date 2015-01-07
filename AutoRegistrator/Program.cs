using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Ionic.Zlib;
using PlasmaDownloader;
using ZkData.UnitSyncLib;
using ZkData;

namespace AutoRegistrator
{
    static class Program
    {

        public static SpringPaths Paths;
        public static SpringScanner Scanner;
        public static PlasmaDownloader.PlasmaDownloader Downloader;

        public class Config: IPlasmaDownloaderConfig {
            public int RepoMasterRefresh { get { return 20; } }
            public string PackageMasterUrl { get { return " http://repos.springrts.com/"; } }
        }




        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            Paths = new SpringPaths(null);
            Scanner = new SpringScanner(Paths);
            
            Scanner.LocalResourceAdded += (s, e) => Trace.TraceInformation("New resource found: {0}", e.Item.InternalName);
            Scanner.LocalResourceRemoved += (s, e) => Trace.TraceInformation("Resource removed: {0}", e.Item.InternalName);

            SpringScanner.MapRegistered += (s, e) => Trace.TraceInformation("Map registered: {0}", e.MapName);
            SpringScanner.ModRegistered += (s, e) => Trace.TraceInformation("Mod registered: {0}", e.Data.Name);

            Scanner.Start();
            Downloader = new PlasmaDownloader.PlasmaDownloader(new Config(), Scanner, Paths);
            Downloader.DownloadAdded += (s, e) => Trace.TraceInformation("Download started: {0}", e.Data.Name);
            Downloader.GetAndSwitchEngine(GlobalConst.DefaultEngineOverride); //for ZKL equivalent, see PlasmaShared/GlobalConst.cs
            Downloader.PackagesChanged += Downloader_PackagesChanged;

            while (true) {Thread.Sleep(10000);}
            /*Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());*/
        }

        static object Locker = new object();

        static void Downloader_PackagesChanged(object sender, EventArgs e)
        {
            foreach (var ver in Downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByTag.Keys)) {
                if (ver.EndsWith(":test") || ver.EndsWith(":latest")) {
                    if (!Downloader.PackageDownloader.SelectedPackages.Contains(ver))
                    {
                        Trace.TraceInformation("Selecting package: {0}",ver);
                        Downloader.PackageDownloader.SelectPackage(ver);
                        Downloader.GetResource(DownloadType.MOD, ver);
                    }

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
                    Trace.TraceInformation("Waiting for: {0} - {1} {2}", d.Name, d.TotalProgress, d.TimeRemaining);
                } else if (Scanner.GetWorkCost() > 0) {
                    waiting = true;
                    Trace.TraceInformation("Waiting for scanner: {0}", Scanner.GetWorkCost());
                }
                else waiting = false;
                if (waiting) Thread.Sleep(10000);
            } while (waiting);



            lock (Locker)
            {
                foreach (var id in new ZkDataContext().Missions.Where(x => !x.IsScriptMission && x.ModRapidTag != "" && !x.IsDeleted).Select(x=>x.MissionID).ToList())
                {
                    using (var db = new ZkDataContext())
                    {
                        var mis = db.Missions.Single(x => x.MissionID == id);
                        try
                        {
                            if (!string.IsNullOrEmpty(mis.ModRapidTag))
                            {
                                var latestMod = Downloader.PackageDownloader.GetByTag(mis.ModRapidTag);
                                if (latestMod != null && mis.Mod != latestMod.InternalName)
                                {
                                    mis.Mod = latestMod.InternalName;
                                    Trace.TraceInformation("Updating mission {0} {1} to {2}", mis.MissionID, mis.Name, mis.Mod);
                                    var mu = new MissionUpdater();
                                    Mod modInfo = null;
                                    Scanner.MetaData.GetMod(mis.NameWithVersion, m => { modInfo = m; }, (er) => { }, Paths.SpringVersion);
                                    mis.Revision++;
                                    mu.UpdateMission(db, mis, modInfo);
                                    db.SubmitChanges();
                                }

                            }


                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Failed to update mission {0}: {1}", mis.MissionID, ex);
                        }
                    }
                }
            }
        }
    }
}
