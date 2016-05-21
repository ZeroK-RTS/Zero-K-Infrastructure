using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using LobbyClient;
using Microsoft.Win32;
using PlasmaShared.UnitSyncLib;
using SpringDownloader.Notifications;
using ZeroKLobby.MicroForms;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.Notifications;
using ZkData;
using ZkData.UnitSyncLib;

namespace ZeroKLobby
{
    internal static class Program
    {
        private static readonly object configLock = new object();
        public static AutoJoinManager AutoJoinManager;
        public static bool CloseOnNext;
        public static Config Conf;
        public static FriendManager FriendManager;
        public static SpringieServer SpringieServer = new SpringieServer();
        public static string[] StartupArgs;
        public static string StartupPath = Path.GetDirectoryName(Path.GetFullPath(Application.ExecutablePath));
        public static ToolTipHandler ToolTip;
        public static BattleBar BattleBar { get; private set; }
        public static BattleIconManager BattleIconManager { get; private set; }
        public static BrowserInterop BrowserInterop { get; private set; }
        public static ConnectBar ConnectBar { get; private set; }
        public static PlasmaDownloader.PlasmaDownloader Downloader { get; private set; }
        public static EngineConfigurator EngineConfigurator { get; set; }
        public static MainWindow MainWindow { get; private set; }
        public static ModStore ModStore { get; private set; }
        public static NotifySection NotifySection { get { return MainWindow.NotifySection; } }
        public static SayCommandHandler SayCommandHandler { get; private set; }
        public static SelfUpdater SelfUpdater { get; set; }
        public static ServerImagesHandler ServerImages { get; private set; }
        public static SpringPaths SpringPaths { get; private set; }

        public static ZklSteamHandler SteamHandler { get; private set; }
        public static SpringScanner SpringScanner { get; private set; }
        public static bool IsSteamFolder { get; private set; }

        public static TasClient TasClient { get; private set; }
        public static VoteBar VoteBar { get; private set; }

        public static Spring RunningSpring { get; set; }


        public static PwBar PwBar { get; private set; }

        /// <summary>
        ///     windows only: do we have admin token?
        /// </summary>
        public static bool IsAdmin() {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }


        internal static void LoadConfig() {
            var curConfPath = Path.Combine(StartupPath, Config.ConfigFileName);
            if (File.Exists(curConfPath)) Conf = Config.Load(curConfPath);
            else
            {
                Conf = Config.Load(Path.Combine(SpringPaths.GetMySpringDocPath(), Config.ConfigFileName));
                Conf.IsFirstRun = true; // treat import as a first run
            }
        }

        private static int GetNetVersionFromRegistry() {
            try
            {
                using (
                    var ndpKey =
                        RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                            .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
                {
                    var releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                    return releaseKey;
                }
            }
            catch
            {
                return 0;
            }
        }


        [STAThread]
        public static void Main(string[] args) {
            try
            {
                //Stopwatch stopWatch = new Stopwatch(); stopWatch.Start();
                Trace.Listeners.Add(new ConsoleTraceListener());

               if (Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    var ver = GetNetVersionFromRegistry();
                    if (ver < 378675)
                    {
                        MessageBox.Show(
                            "Zero-K launcher needs Microsoft .NET framework 4.5.1\nPlease download and install it first",
                            "Program is unable to run",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }

                Directory.SetCurrentDirectory(StartupPath);

                // extract fonts
                EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.SM.ttf", "SM.ttf");
                EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.OpenSans-Semibold.ttf", "OpenSans-Semibold.ttf");
                EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.OpenSans-Regular.ttf", "OpenSans-Regular.ttf");

                Conf = new Config();

                IsSteamFolder = File.Exists(Path.Combine(StartupPath, "steamfolder.txt"));

                SelfUpdater = new SelfUpdater("Zero-K");

                StartupArgs = args;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (!Debugger.IsAttached)
                {
                    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                    Thread.GetDomain().UnhandledException += UnhandledException;
                    Application.ThreadException += Application_ThreadException;
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                }

                //HttpWebRequest.DefaultCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                Trace.TraceInformation("Starting with version {0}", SelfUpdater.CurrentVersion);

                WebRequest.DefaultWebProxy = null;
                ThreadPool.SetMaxThreads(500, 2000);
                ServicePointManager.Expect100Continue = false;
                LoadConfig();
                Trace.Listeners.Add(new LogTraceListener());
                if (Environment.OSVersion.Platform != PlatformID.Unix && !Conf.UseExternalBrowser) Utils.SetIeCompatibility(); //set to current IE version

                var contentDir = !string.IsNullOrEmpty(Conf.DataFolder) ? Conf.DataFolder : StartupPath;
                if (!Directory.Exists(contentDir) || !SpringPaths.IsDirectoryWritable(contentDir))
                {
                    var dc = new SelectWritableFolder { SelectedPath = SpringPaths.GetMySpringDocPath() };
                    if (dc.ShowDialog() != DialogResult.OK) return;
                    contentDir = dc.SelectedPath;
                }
                if (Conf.DataFolder != StartupPath) Conf.DataFolder = contentDir;
                else Conf.DataFolder = null;

                if (!SpringPaths.IsDirectoryWritable(StartupPath))
                {
                    var newTarget = Path.Combine(contentDir, "Zero-K.exe");
                    if (SelfUpdater.CheckForUpdate(newTarget, true))
                    {
                        Conf.Save(Path.Combine(contentDir, Config.ConfigFileName));
                        Process.Start(newTarget);
                        return;
                    } MessageBox.Show("Move failed, please copy Zero-K.exe to a writable folder");
                    return;
                }

                

                SpringPaths = new SpringPaths(null, contentDir);
                SpringPaths.MakeFolders();

                // speed up spring start
                SpringPaths.SpringVersionChanged += (sender, eventArgs) =>
                {
                    ZkData.Utils.StartAsync(
                        () =>
                        {
                            UnitSync unitSync = null;
                            try
                            {
                                unitSync = new UnitSync(SpringPaths); // initialize unitsync to avoid slowdowns when starting

                                if (unitSync.UnitsyncWritableFolder != SpringPaths.WritableDirectory)
                                {
                                    // unitsync created its cache in different folder than is used to start spring -> move it
                                    var fi = ArchiveCache.GetCacheFile(unitSync.UnitsyncWritableFolder);
                                    if (fi != null) File.Copy(fi.FullName, Path.Combine(SpringPaths.WritableDirectory, "cache", fi.Name), true);
                                }
                            }
                            finally
                            {
                                unitSync?.Dispose();
                            }
                        });
                };

                SaveConfig();


                // write license files
                try
                {
                    var path = SpringPaths.WritableDirectory;
                    var pathGPL = Utils.MakePath(path, "license_GPLv3");
                    var gpl = Encoding.UTF8.GetString(License.GPLv3);
                    if (!File.Exists(pathGPL)) File.WriteAllText(pathGPL, gpl);
                    var pathMIT = Utils.MakePath(path, "license_MIT");
                    var mit = Encoding.UTF8.GetString(License.MITlicense);
                    if (!File.Exists(pathMIT)) File.WriteAllText(pathMIT, mit);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }


                
                if (Conf.IsFirstRun)
                {
                    if (!IsSteamFolder)
                    {
                        Utils.CreateDesktopShortcut();
                    }
                    if (Environment.OSVersion.Platform != PlatformID.Unix) Utils.RegisterProtocol();
                }

                FriendManager = new FriendManager();
                AutoJoinManager = new AutoJoinManager();
                EngineConfigurator = new EngineConfigurator(SpringPaths.WritableDirectory);

                SpringScanner = new SpringScanner(SpringPaths);
                SpringScanner.LocalResourceAdded += (s, e) => Trace.TraceInformation("New resource found: {0}", e.Item.InternalName);
                SpringScanner.LocalResourceRemoved += (s, e) => Trace.TraceInformation("Resource removed: {0}", e.Item.InternalName);
                if (Conf.EnableUnitSyncPrompt && Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    SpringScanner.UploadUnitsyncData += UnitSyncUploadPrompt.SpringScanner_UploadUnitsyncData;
                    SpringScanner.RetryResourceCheck += UnitSyncRetryPrompt.SpringScanner_RetryGetResourceInfo;
                }

                SpringScanner.MapRegistered += (s, e) => Trace.TraceInformation("Map registered: {0}", e.MapName);
                SpringScanner.ModRegistered += (s, e) => Trace.TraceInformation("Mod registered: {0}", e.Data.Name);

                Downloader = new PlasmaDownloader.PlasmaDownloader(SpringScanner, SpringPaths); //rapid
                Downloader.DownloadAdded += (s, e) => Trace.TraceInformation("Download started: {0}", e.Data.Name);
                Downloader.GetAndSwitchEngine(GlobalConst.DefaultEngineOverride ?? TasClient.ServerSpringVersion);

                var isLinux = Environment.OSVersion.Platform == PlatformID.Unix;
                TasClient = new TasClient(string.Format("ZK {0}{1}", SelfUpdater.CurrentVersion, isLinux ? " linux" : ""));

                SayCommandHandler = new SayCommandHandler(TasClient);

                ServerImages = new ServerImagesHandler(SpringPaths, TasClient);


                // log, for debugging
                TasClient.Connected += (s, e) => Trace.TraceInformation("TASC connected");
                TasClient.LoginAccepted += (s, e) =>
                {
                    Trace.TraceInformation("TASC login accepted");
                    Trace.TraceInformation("Server is using Spring version {0}", TasClient.ServerSpringVersion);
                    if (Environment.OSVersion.Platform == PlatformID.Unix || Conf.UseExternalBrowser) if (MainWindow != null) MainWindow.navigationControl.Path = "battles";
                };

                TasClient.LoginDenied += (s, e) => Trace.TraceInformation("TASC login denied");
                TasClient.ChannelJoined += (s, e) => { Trace.TraceInformation("TASC channel joined: " + e.Name); };
                TasClient.ConnectionLost += (s, e) => Trace.TraceInformation("Connection lost");

                // special handling
                TasClient.PreviewSaid += (s, e) =>
                {
                    var tas = (TasClient)s;
                    User user = null;
                    if (e.Data.UserName != null)
                    {
                        tas.ExistingUsers.TryGetValue(e.Data.UserName, out user);
                        if ((user != null && user.BanMute) || Conf.IgnoredUsers.Contains(e.Data.UserName)) e.Cancel = true;
                    }
                };

                TasClient.SiteToLobbyCommandReceived += (eventArgs, o) =>
                {
                    if (MainWindow != null)
                    {
                        MainWindow.navigationControl.Path = o.Command;
                        MainWindow.PopupSelf();
                    }
                };

                ModStore = new ModStore();

                ConnectBar = new ConnectBar(TasClient);
                ToolTip = new ToolTipHandler();
                BrowserInterop = new BrowserInterop(TasClient, Conf);
                BattleIconManager = new BattleIconManager();
                Application.AddMessageFilter(ToolTip);

                SteamHandler = new ZklSteamHandler(TasClient);

                MainWindow = new MainWindow();

                Application.AddMessageFilter(new ScrollMessageFilter());

                MainWindow.Size = new Size(
                    Math.Min(SystemInformation.VirtualScreen.Width - 30, MainWindow.Width),
                    Math.Min(SystemInformation.VirtualScreen.Height - 30, MainWindow.Height)); //in case user have less space than 1024x768

                BattleBar = new BattleBar();
                VoteBar = new VoteBar();
                PwBar = new PwBar();

           
                if (!Debugger.IsAttached && !Conf.DisableAutoUpdate && !IsSteamFolder) SelfUpdater.StartChecking();

                SteamHandler.Connect();
                Application.Run(MainWindow);

                ShutDown();
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleException(ex, true);
                if (Debugger.IsAttached) Debugger.Break();
            }
            finally
            {
                ShutDown();
            }
            if (ErrorHandling.HasFatalException && !CloseOnNext)
            {
                if (Debugger.IsAttached) Debugger.Break();
                Application.Restart();
            }
        }

        internal static void SaveConfig() {
            lock (configLock)
            {
                Conf.Save(Path.Combine(StartupPath, Config.ConfigFileName));
            }
        }

        private static void FinalizeShutdown() {
            HistoryManager.FlushBuffer();
            Conf.IsFirstRun = false;

            if (Conf.DiscardPlayerName) Conf.LobbyPlayerName = "";
            if (Conf.DiscardPassword) Conf.LobbyPlayerPassword = "";

            SaveConfig();

            try
            {
                if (ToolTip != null) ToolTip.Dispose();
                if (Downloader != null) Downloader.Dispose();
                if (SpringScanner != null) SpringScanner.Dispose();
                if (SteamHandler != null) SteamHandler.Dispose();
            }
            catch {}
        }

        public static void ShutDown() {
            CloseOnNext = true;
            FinalizeShutdown();
            //Thread.Sleep(5000);
            Application.Exit();
        }

        public static void Restart() {
            CloseOnNext = true;
            FinalizeShutdown();
            //Thread.Sleep(5000);
            Application.Restart();
        }

        private static void TasClientInvoker(TasClient.Invoker a) {
            if (!CloseOnNext) MainWindow.InvokeFunc(() => a());
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
            try
            {
                ErrorHandling.HandleException(e.Exception, true);
                if (Debugger.IsAttached) Debugger.Break();
            }
            catch {}
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            try
            {
                ErrorHandling.HandleException((Exception)e.ExceptionObject, e.IsTerminating);
                if (Debugger.IsAttached) Debugger.Break();
            }
            catch {}
        }


        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            try
            {
                ErrorHandling.HandleException((Exception)e.ExceptionObject, e.IsTerminating);
                if (Debugger.IsAttached) Debugger.Break();
            }
            catch {}
        }
    }
}