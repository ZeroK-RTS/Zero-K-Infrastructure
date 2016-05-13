using System;
using System.Collections.Generic;
using System.Linq;
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
            TcpTransport connection = null;

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

            CefWrapper.Initialize(startupPath + "/render", args);


            var springScanner = new SpringScanner(springPaths);
            springScanner.Start();

            EventHandler<ProgressEventArgs> workHandler =
                (s, e) => { CefWrapper.ExecuteJavascript("on_spring_scanner_work(" + JsonConvert.SerializeObject(e) + ");"); };
            springScanner.WorkStarted += workHandler;
            springScanner.WorkProgressChanged += workHandler;
            springScanner.WorkStopped += (s, e) => { CefWrapper.ExecuteJavascript("on_spring_scanner_work(null);"); };
            springScanner.LocalResourceAdded +=
                (s, e) => { CefWrapper.ExecuteJavascript("on_spring_scanner_add(" + JsonConvert.SerializeObject(e.Item) + ")"); };
            springScanner.LocalResourceRemoved +=
                (s, e) => { CefWrapper.ExecuteJavascript("on_spring_scanner_remove(" + JsonConvert.SerializeObject(e.Item) + ")"); };


            var downloader = new PlasmaDownloader.PlasmaDownloader(springScanner, springPaths); //rapid
            downloader.GetAndSwitchEngine(GlobalConst.DefaultEngineOverride);

            // ZKL's downloader doesn't send events to monitor download progress, so we have to poll it.
            Timer pollDownloads = new Timer();
            pollDownloads.Interval = 250;
            pollDownloads.Tick += (s, e) => {
                CefWrapper.ExecuteJavascript("on_downloads_change(" + JsonConvert.SerializeObject(downloader.Downloads) + ")");
            };
            // Through some WinAPI dark magic it manages to use the message pump in the window that is run by CEF.
            // Can this be dangerous?
            pollDownloads.Start();


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
                "abortDownload",
                (string name) =>
                {
                    downloader.Downloads.FirstOrDefault(d => d.Name == name)?.Abort();
                });

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

            CefWrapper.RegisterApiFunction(
                "connect",
                (string host, int port) =>
                {
                    if (connection != null) connection.RequestClose();
                    connection = new TcpTransport(host, port);
                    connection.ConnectAndRun(
                        async (s) => CefWrapper.ExecuteJavascript(
                            $"on_lobby_message({CefWrapper.mangleUtf8(JsonConvert.SerializeObject(s))})"),
                        async () => { },
                        async (requested) => CefWrapper.ExecuteJavascript(
                            $"on_connection_closed({CefWrapper.mangleUtf8(JsonConvert.SerializeObject(requested))})")
                        );
                });
            CefWrapper.RegisterApiFunction("disconnect", () => connection?.RequestClose());
            CefWrapper.RegisterApiFunction("sendLobbyMessage", (string msg) => connection?.SendLine(CefWrapper.unmangleUtf8(msg) + '\n'));

            CefWrapper.RegisterApiFunction(
                "readConfig",
                () =>
                {
                    try { return JsonConvert.DeserializeObject(File.ReadAllText(startupPath + "/config.json")); }
                    catch(FileNotFoundException) { return null; }
                });
            CefWrapper.RegisterApiFunction("saveConfig", (object conf) => File.WriteAllText(startupPath + "/config.json",
                JsonConvert.SerializeObject(conf, Formatting.Indented)));

            var fileUrl = new Uri(startupPath + "/zkwl/index.html");
            CefWrapper.StartMessageLoop(fileUrl.AbsoluteUri, "black", true);
            CefWrapper.Deinitialize();

            downloader.Dispose();
            springScanner.Dispose();
        }

    }
}