using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using LobbyClient;
using PlasmaShared;
using SpringDownloader.Notifications;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.Notifications;
using ZkData;

namespace ZeroKLobby
{
    static class Program
    {
        static readonly object configLock = new object();
        static string ConfigDirectory;
        static NewVersionBar NewVersionBar;
        static Mutex mutex;
        public static AutoJoinManager AutoJoinManager;
        public static BattleBar BattleBar { get; private set; }
        public static BattleIconManager BattleIconManager { get; private set; }
        public static BrowserInterop BrowserInterop { get; private set; }
        public static bool CloseOnNext;
        public static Config Conf = new Config();
        public static ConnectBar ConnectBar { get; private set; }
        public static PlasmaDownloader.PlasmaDownloader Downloader { get; private set; }
        public static EngineConfigurator EngineConfigurator { get; set; }
        public static FriendManager FriendManager;
        public static JugglerBar JugglerBar { get; private set; }
        public static MainWindow MainWindow { get; private set; }
        public static ModStore ModStore { get; private set; }
        public static NotifySection NotifySection { get { return MainWindow.NotifySection; } }
        public static SayCommandHandler SayCommandHandler { get; private set; }
        public static SelfUpdater SelfUpdater { get; set; }
        public static ServerImagesHandler ServerImages { get; private set; }
        public static SpringPaths SpringPaths { get; private set; }
        public static SpringScanner SpringScanner { get; private set; }
        public static SpringieServer SpringieServer = new SpringieServer();
        public static string[] StartupArgs;
        public static string StartupPath = Path.GetDirectoryName(Path.GetFullPath(Application.ExecutablePath));
        public static TasClient TasClient { get; private set; }
        public static ToolTipHandler ToolTip;
        public static VoteBar VoteBar { get; private set; }

        /// <summary>
        /// windows only: do we have admin token?
        /// </summary>
        public static bool IsAdmin() {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static void LoadConfig() {
            var configFilename = GetFullConfigPath();
            if (!File.Exists(configFilename)) {
                // port old config      
                /*if (ApplicationDeployment.IsNetworkDeployed)
                {
                    try
                    {
                        File.Move(Path.Combine(ApplicationDeployment.CurrentDeployment.DataDirectory, "SpringDownloaderConfig.xml"), configFilename);
                    }
                    catch {}
                }*/
            }

            if (File.Exists(configFilename)) {
                var xs = new XmlSerializer(typeof(Config));
                try {
                    Conf = (Config)xs.Deserialize(new StringReader(File.ReadAllText(configFilename)));
                    Conf.IsFirstRun = false;
                } catch (Exception ex) {
                    Trace.TraceError("Error reading config file: {0}", ex);
                    Conf = new Config();
                    Conf.IsFirstRun = true;
                }
            }
            else Conf.IsFirstRun = true;

            Conf.UpdateFadeColor();
        }

        [STAThread]
        public static void Main(string[] args) {
            try {
                Trace.Listeners.Add(new ConsoleTraceListener());
                Trace.Listeners.Add(new LogTraceListener());

                if (Process.GetProcesses().Any(x => x.ProcessName.StartsWith("spring_"))) return; // dont start if started from installer

                StartupArgs = args;

                Directory.SetCurrentDirectory(StartupPath);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (!Debugger.IsAttached) {
                    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                    Thread.GetDomain().UnhandledException += UnhandledException;
                    Application.ThreadException += Application_ThreadException;
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                }

                //HttpWebRequest.DefaultCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

                Utils.RegisterProtocol();

                SelfUpdater = new SelfUpdater("Zero-K");

                Trace.TraceInformation("Starting with version {0}", SelfUpdater.CurrentVersion);

                WebRequest.DefaultWebProxy = null;
                ThreadPool.SetMaxThreads(500, 2000);
                ServicePointManager.Expect100Continue = false;

                LoadConfig();

                SpringPaths = new SpringPaths(Conf.ManualSpringPath, null, Conf.DataFolder);

                Conf.DataFolder = SpringPaths.WritableDirectory;
                SpringPaths.MakeFolders();

                SaveConfig();

                SpringPaths.SpringVersionChanged += (s, e) =>
                    {
                        Conf.ManualSpringPath = Path.GetDirectoryName(SpringPaths.Executable);
                        SaveConfig();
                    };

                try {
                    if (!Debugger.IsAttached) {
                        var wp = "";
                        foreach (var c in SpringPaths.WritableDirectory.Where(x => (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z'))) wp += c;
                        mutex = new Mutex(false, "ZeroKLobby" + wp);
                        if (!mutex.WaitOne(10000, false)) {
                            MessageBox.Show(
                                "Another copy of Zero-K lobby is still running" +
                                "\nMake sure the other lobby is closed (check task manager) before starting new one",
                                "There can be only one lobby running",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Stop);
                            return;
                        }
                    }
                } catch (AbandonedMutexException) {}

                FriendManager = new FriendManager();
                AutoJoinManager = new AutoJoinManager();
                EngineConfigurator = new EngineConfigurator(SpringPaths.WritableDirectory);

                SpringScanner = new SpringScanner(SpringPaths);
                SpringScanner.LocalResourceAdded += (s, e) => Trace.TraceInformation("New resource found: {0}", e.Item.InternalName);
                SpringScanner.LocalResourceRemoved += (s, e) => Trace.TraceInformation("Resource removed: {0}", e.Item.InternalName);

                SpringScanner.MapRegistered += (s, e) => Trace.TraceInformation("Map registered: {0}", e.MapName);
                SpringScanner.ModRegistered += (s, e) => Trace.TraceInformation("Mod registered: {0}", e.Data.Name);

                Downloader = new PlasmaDownloader.PlasmaDownloader(Conf, SpringScanner, SpringPaths);
                Downloader.DownloadAdded += (s, e) => Trace.TraceInformation("Download started: {0}", e.Data.Name);

                TasClient = new TasClient(TasClientInvoker, string.Format("ZK {0}", SelfUpdater.CurrentVersion), GlobalConst.ZkLobbyUserCpu, true);

                SayCommandHandler = new SayCommandHandler(TasClient);

                ServerImages = new ServerImagesHandler(SpringPaths, TasClient);


                // log, for debugging
                TasClient.Connected += (s, e) => Trace.TraceInformation("TASC connected");
                TasClient.LoginAccepted += (s, e) =>
                    {
                        Trace.TraceInformation("TASC login accepted");
                        Trace.TraceInformation("Server is using Spring version {0}", TasClient.ServerSpringVersion);
                        if (SpringPaths.SpringVersion != TasClient.ServerSpringVersion) Downloader.GetAndSwitchEngine(TasClient.ServerSpringVersion);
                        MainWindow.navigationControl.Path = "chat/channel/zk"; // todo ugly    
                    };

                TasClient.LoginDenied += (s, e) => Trace.TraceInformation("TASC login denied");
                TasClient.ChannelJoined += (s, e) => { Trace.TraceInformation("TASC channel joined: " + e.ServerParams[0]); };
                TasClient.ConnectionLost += (s, e) => Trace.TraceInformation("Connection lost");

                // special handling
                TasClient.PreviewSaid += (s, e) =>
                    {
                        var tas = (TasClient)s;
                        User user = null;
                        tas.ExistingUsers.TryGetValue(e.Data.UserName, out user);
                        if ((user != null && user.BanMute) || Conf.IgnoredUsers.Contains(e.Data.UserName)) e.Cancel = true;
                    };

                TasClient.Extensions.JsonDataReceived += (eventArgs, o) =>
                    {
                        var command = o as ProtocolExtension.SiteToLobbyCommand;
                        if (command != null) MainWindow.navigationControl.Path = command.SpringLink;
                    };

                ConnectBar = new ConnectBar(TasClient);
                ModStore = new ModStore();
                ToolTip = new ToolTipHandler();
                JugglerBar = new JugglerBar(TasClient);
                BrowserInterop = new BrowserInterop();

                Application.AddMessageFilter(ToolTip);

                MainWindow = new MainWindow();

                Application.AddMessageFilter(new ScrollMessageFilter());
                
                
                if (Conf.StartMinimized) MainWindow.WindowState = FormWindowState.Minimized;
                else MainWindow.WindowState = FormWindowState.Normal;

                BattleIconManager = new BattleIconManager(MainWindow);
                BattleBar = new BattleBar();
                NewVersionBar = new NewVersionBar(SelfUpdater);
                VoteBar = new VoteBar();

                //This make the size of every bar constant (only for height).
                //This is a HAX, we wanted to make them constant because the bar will be DPI-scaled twice/thrice/multiple-time again somewhere but we don't know where it is to fix them.
                // todo wtf fix this crazy thing
                var votebarSize = new Size(0, VoteBar.Height);
                // Reference: http://stackoverflow.com/questions/5314041/set-minimum-window-size-in-c-sharp-net
                var newversionbarSize = new Size(0, NewVersionBar.Height);
                var battlebarSize = new Size(0, BattleBar.Height);
                var connectbarSize = new Size(0, ConnectBar.Height);
                var jugglerbarSize = new Size(0, JugglerBar.Height);

                VoteBar.MinimumSize = votebarSize; //fix minimum size forever
                VoteBar.MaximumSize = votebarSize; //fix maximum size forever
                NewVersionBar.MinimumSize = newversionbarSize;
                NewVersionBar.MaximumSize = newversionbarSize;
                BattleBar.MinimumSize = battlebarSize;
                BattleBar.MaximumSize = battlebarSize;
                ConnectBar.MinimumSize = connectbarSize;
                ConnectBar.MaximumSize = connectbarSize;
                JugglerBar.MinimumSize = jugglerbarSize;
                JugglerBar.MaximumSize = jugglerbarSize;
                //End battlebar size fix hax

                if (!Debugger.IsAttached) Program.SelfUpdater.StartChecking();

                if (Conf.ShowFriendsWindow == true) {
                    MainWindow.frdWindow = new FriendsWindow();
                    MainWindow.frdWindow.Show();
                    FriendsWindow.Creatable = false;
                }

                Application.Run(MainWindow);
                ShutDown();
            } catch (Exception ex) {
                ErrorHandling.HandleException(ex, true);
                Trace.TraceError("Error in application:" + ex);
            }
        }

        internal static void SaveConfig() {
            var configFilename = GetFullConfigPath();
            lock (configLock) {
                var cols = new StringCollection();
                cols.AddRange(Conf.AutoJoinChannels.OfType<string>().Distinct().ToArray());
                Conf.AutoJoinChannels = cols;
                var xs = new XmlSerializer(typeof(Config));
                var sb = new StringBuilder();
                using (var stringWriter = new StringWriter(sb)) xs.Serialize(stringWriter, Conf);
                File.WriteAllText(configFilename, sb.ToString());
            }
        }

        public static void ShutDown() {
            try {
                if (!FriendsWindow.Creatable) MainWindow.frdWindow.Close();
                if (!Debugger.IsAttached) mutex.ReleaseMutex();
            } catch {}
            if (ToolTip != null) ToolTip.Dispose();
            if (Downloader != null) Downloader.Dispose();
            if (SpringScanner != null) SpringScanner.Dispose();
            Thread.Sleep(5000);
        }


        static string GetFullConfigPath() {
            if (ConfigDirectory == null) {
                //detect configuration path once
                if (Debugger.IsAttached) {
                    if (SpringPaths.IsDirectoryWritable(StartupPath)) {
                        //use startup path when on linux
                        //or if startup path is writable on windows
                        ConfigDirectory = StartupPath;
                    }
                    else {
                        //if we are on windows and startup path isnt writable, use my documents/games/spring
                        ConfigDirectory = SpringPaths.GetMySpringDocPath();
                    }
                }
                else ConfigDirectory = SpringPaths.GetMySpringDocPath();
            }

            return Path.Combine(ConfigDirectory, Config.ConfigFileName);
        }


        static void TasClientInvoker(TasClient.Invoker a) {
            if (!CloseOnNext) MainWindow.InvokeFunc(() => a());
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
            try {
                ErrorHandling.HandleException(e.Exception, true);
                Trace.TraceError("unhandled exception: {0}", e.Exception);
            } catch {}
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            try {
                ErrorHandling.HandleException((Exception)e.ExceptionObject, e.IsTerminating);
                Trace.TraceError("unhandled exception: {0}", e.ExceptionObject);
            } catch {}
        }


        static void UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            try {
                ErrorHandling.HandleException((Exception)e.ExceptionObject, e.IsTerminating);
                Trace.TraceError("unhandled exception: {0}", e.ExceptionObject);
            } catch {}
        }
    }
}