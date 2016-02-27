using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using LobbyClient;
using Newtonsoft.Json;
using PlasmaDownloader;
using PlasmaShared.UnitSyncLib;
using ZeroKLobby;
using ZkData;
using ZkData.UnitSyncLib;

namespace ZkWebLobby
{
    internal static class Program
    {
        //[STAThread]
        private static void Main(params string[] args) {
            var startupPath = Path.GetDirectoryName(Path.GetFullPath(Application.ExecutablePath));
            var springPaths = new SpringPaths(null, startupPath);
            Spring runningSpring = null;
            springPaths.MakeFolders();

            // speed up spring start
            springPaths.SpringVersionChanged += (sender, eventArgs) =>
            {
                Utils.StartAsync(
                    () =>
                    {
                        UnitSync unitSync = null;
                        try
                        {
                            unitSync = new UnitSync(springPaths); // initialize unitsync to avoid slowdowns when starting

                            if (unitSync.UnitsyncWritableFolder != springPaths.WritableDirectory)
                            {
                                // unitsync created its cache in different folder than is used to start spring -> move it
                                var fi = ArchiveCache.GetCacheFile(unitSync.UnitsyncWritableFolder);
                                if (fi != null) File.Copy(fi.FullName, Path.Combine(springPaths.WritableDirectory, "cache", fi.Name), true);
                            }
                        }
                        finally
                        {
                            unitSync?.Dispose();
                        }
                    });
            };

            var springScanner = new SpringScanner(springPaths);

            var downloader = new PlasmaDownloader.PlasmaDownloader(new DownloaderConfig(), springScanner, springPaths); //rapid
            downloader.GetAndSwitchEngine(GlobalConst.DefaultEngineOverride);

            springScanner.Start();

            var fileUrl = new Uri(startupPath + "/zkwl/index.html");

            CefWrapper.Initialize(startupPath + "/render", args);
            EventHandler<ProgressEventArgs> workHandler =
                (s, e) => { CefWrapper.ExecuteJavascript("on_spring_scanner_work(" + JsonConvert.SerializeObject(e) + ");"); };
            springScanner.WorkStarted += workHandler;
            springScanner.WorkProgressChanged += workHandler;
            springScanner.WorkStopped += (s, e) => { CefWrapper.ExecuteJavascript("on_spring_scanner_work(null);"); };
            springScanner.LocalResourceAdded +=
                (s, e) => { CefWrapper.ExecuteJavascript("on_spring_scanner_add(" + JsonConvert.SerializeObject(e.Item) + ")"); };
            springScanner.LocalResourceRemoved +=
                (s, e) => { CefWrapper.ExecuteJavascript("on_spring_scanner_remove(" + JsonConvert.SerializeObject(e.Item) + ")"); };

            CefWrapper.RegisterApiFunction(
                "getEngines",
                () => {
                    return new List<string> { "100.0" }; // TODO: stub
                });
            CefWrapper.RegisterApiFunction("getMods", () => { return springScanner.GetAllModResource(); });
            CefWrapper.RegisterApiFunction("getMaps", () => { return springScanner.GetAllMapResource(); });
            CefWrapper.RegisterApiFunction(
                "downloadEngine",
                (string engine) =>
                {
                    // Don't let GetAndSwitchEngine() touch the main SpringPaths.
                    var path = new SpringPaths(springPaths.GetEngineFolderByVersion(engine), springPaths.WritableDirectory);
                    downloader.GetAndSwitchEngine(engine, path);
                });
            CefWrapper.RegisterApiFunction("downloadMod", (string game) => { downloader.GetResource(DownloadType.MOD, game); });
            CefWrapper.RegisterApiFunction("downloadMap", (string map) => { downloader.GetResource(DownloadType.MAP, map); });
            CefWrapper.RegisterApiFunction(
                "startSpringScript",
                (string engineVer, string script) =>
                {
                    if (runningSpring != null) return null;
                    // Ultimately we should get rid of the concept of a "current set engine", but for now let's work around it.
                    var path = new SpringPaths(springPaths.GetEngineFolderByVersion(engineVer), springPaths.WritableDirectory);
                    runningSpring = new Spring(path);
                    runningSpring.SpringExited += (obj, evt) =>
                    {
                        CefWrapper.ExecuteJavascript("on_spring_exit(" + (evt.Data ? "true" : "false") + ");");
                        runningSpring = null;
                    };
                    try
                    {
                        runningSpring.StartSpring(script);
                        return null;
                    }
                    catch (Exception e)
                    {
                        runningSpring = null;
                        return e.Message;
                    }
                });

            CefWrapper.StartMessageLoop(fileUrl.AbsoluteUri, "black", true);
            CefWrapper.Deinitialize();

            downloader.Dispose();
            springScanner.Dispose();
        }

        public class DownloaderConfig : IPlasmaDownloaderConfig
        {
            public string PackageMasterUrl => "http://repos.springrts.com/";
            public int RepoMasterRefresh => 0;
        }
    }
}