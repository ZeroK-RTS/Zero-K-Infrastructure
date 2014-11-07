using System; 
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using PlasmaDownloader;
using PlasmaShared;
using SpringDownloader.Notifications;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.Notifications;

namespace ZeroKLobby
{
    public partial class MainWindow: Form
    {

        string baloonTipPath = null;

        readonly ToolStripMenuItem btnExit;
        readonly ToolStripMenuItem btnFriends;
        readonly ToolStripSeparator btnSepertator;

        bool closeForReal;
        FormWindowState lastState = FormWindowState.Normal;
        
        readonly NotifyIcon systrayIcon;
        readonly Timer timer1 = new Timer();
        readonly ContextMenuStrip trayStrip;
        public NavigationControl navigationControl { get { return navigationControl1; } }
        public ChatTab ChatTab { get { return  navigationControl1.ChatTab; } }
        public static MainWindow Instance { get; private set; }

        public NotifySection NotifySection { get { return notifySection1; } }
        public static FriendsWindow frdWindow = null;
        
        public enum Platform // Zero-K lobby probably already has some global var like this somewhere
        {
           Windows,
           Linux,
           Mac
        }
        public Platform MyOS = Platform.Windows; // Which will most likely to be the case for most community

        public MainWindow() {
            InitializeComponent();
            //Invalidate(true);
            Instance = this;
            //systrayIcon.BalloonTipClicked += systrayIcon_BalloonTipClicked;
            
            switch (Environment.OSVersion.Platform)
            {
            case PlatformID.Unix:
               // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
               // Instead of platform check, we'll do a feature checks (Mac specific root folders)
               if (System.IO.Directory.Exists ("/Applications")
                   & System.IO.Directory.Exists ("/System")
                   & System.IO.Directory.Exists ("/Users")
                   & System.IO.Directory.Exists ("/Volumes"))
                  MyOS = Platform.Mac;
               else
                  MyOS = Platform.Linux;
               break;
            case PlatformID.MacOSX:
               MyOS = Platform.Mac;
               break;
            default:
               MyOS = Platform.Windows;
               break;
            }

            btnExit = new ToolStripMenuItem { Name = "btnExit", Size = new Size(92, 22), Text = "Exit" };
            btnExit.Click += btnExit_Click;

            btnFriends = new ToolStripMenuItem { Name = "btnFriends", Size = new Size(92, 22), Text = "Show Friends" };
            btnFriends.Click += btnFriends_Click;

            btnSepertator = new ToolStripSeparator { Name = "btnSprtr" };

            trayStrip = new ContextMenuStrip();
            trayStrip.Items.AddRange(new ToolStripItem[] { btnFriends, btnSepertator, btnExit });
            trayStrip.Name = "trayStrip";
            trayStrip.Size = new Size(93, 26);

            systrayIcon = new NotifyIcon { ContextMenuStrip = trayStrip, Text = "Zero-K", Visible = true };
            systrayIcon.MouseDown += systrayIcon_MouseDown;
            systrayIcon.BalloonTipClicked += systrayIcon_BalloonTipClicked;

            if (Program.Downloader != null) {
                timer1.Interval = 250;
                timer1.Tick += timer1_Tick;

                Program.Downloader.DownloadAdded += TorrentManager_DownloadAdded;
                timer1.Start();
            }
            ReloadPosition();
        }

        private void ReloadPosition()
        {
			MinimumSize = new Size(200, 300); //so splitcontainer in SettingTab dont throw exception when pushed too close together
            Size windowSize = Program.Conf.windowSize.IsEmpty ? Size : Program.Conf.windowSize; //Note: default MainWindow size is 1024x768 defined in MainWindow.Designer.cs
            windowSize = new Size(Math.Min(SystemInformation.VirtualScreen.Width - 30, windowSize.Width),
                           Math.Min(SystemInformation.VirtualScreen.Height - 30, windowSize.Height)); //in case user have less space than 1024x768
            Point windowLocation = Program.Conf.windowLocation.IsEmpty ? DesktopLocation : Program.Conf.windowLocation;
            windowLocation = new Point(Math.Min(SystemInformation.VirtualScreen.Width - windowSize.Width / 2, windowLocation.X),
                                 Math.Min(SystemInformation.VirtualScreen.Height - windowSize.Height / 2, windowLocation.Y)); //in case user changed resolution
            windowLocation = new Point(Math.Max(0 - windowSize.Width / 2, windowLocation.X),
                                        Math.Max(0 - windowSize.Height / 2, windowLocation.Y));
            StartPosition = FormStartPosition.Manual; //use manual to allow programmatic re-positioning
            Size = windowSize;
            DesktopLocation = windowLocation;
        }
        
        private void SavePosition()
        {
            Program.Conf.windowSize = Size;
            Program.Conf.windowLocation = DesktopLocation;
        }

        public void DisplayLog() {
            if (!FormLog.Instance.Visible) {
                FormLog.Instance.Visible = true;
                FormLog.Instance.Focus();
            }
            else FormLog.Instance.Visible = false;
        }


        public void Exit() {
            if (closeForReal) return;
            closeForReal = true;
            Program.CloseOnNext = true;
            InvokeFunc(() => { systrayIcon.Visible = false; });
            InvokeFunc(Close);
        }

        public Control GetHoveredControl() {
            //fore control
            var lastForm = Application.OpenForms.OfType<Form>().LastOrDefault(x => !(x is ToolTipForm) && x.Visible);
            if (lastForm != null) {
                var hovered = lastForm.GetHoveredControl();
                if (hovered != null) 
                    return hovered;

                //back control (note: double tries so that tooltip from multiple window layer can be displayed at once)
                lastForm = Application.OpenForms.OfType<Form>().FirstOrDefault(x => !(x is ToolTipForm) && x.Visible);
                if (lastForm != null) {
                    hovered = lastForm.GetHoveredControl();
                    if (hovered != null) return hovered;
                }
            }
            return null;
        }

        public void InvokeFunc(Action funcToInvoke) {
            try {
                if (InvokeRequired) Invoke(funcToInvoke);
                else funcToInvoke();
            } catch (Exception ex) {
                Trace.TraceError("Error invoking: {0}", ex);
            }
        }

        /// <summary>
        /// Alerts user
        /// </summary>
        /// <param name="navigationPath">navigation path of event - alert is set on this and disabled if users goes there</param>
        /// <param name="message">bubble message - setting null means no bubble</param>
        /// <param name="useSound">use sound notification</param>
        /// <param name="useFlashing">use flashing</param>
        public void NotifyUser(string navigationPath, string message, bool useSound = false, bool useFlashing = false) {
            var showBalloon =
                !((Program.Conf.DisableChannelBubble && navigationPath.Contains("chat/channel/")) ||
                  (Program.Conf.DisablePmBubble && navigationPath.Contains("chat/user/")));

            var isHidden = WindowState == FormWindowState.Minimized || Visible == false || ActiveForm == null;
            var isPathDifferent = navigationControl.Path != navigationPath;

            if (isHidden || isPathDifferent) {
                if (!string.IsNullOrEmpty(message)) {
                    baloonTipPath = navigationPath;
                    if (showBalloon) systrayIcon.ShowBalloonTip(5000, "Zero-K", message, ToolTipIcon.Info);
                }
            }
            if (isHidden && useFlashing) FlashWindow();
            if (isPathDifferent) navigationControl.HilitePath(navigationPath, useFlashing ? HiliteLevel.Flash : HiliteLevel.Bold);
            if (useSound)
            {
                if (MyOS == Platform.Windows) {
                    try
                    {
                        SystemSounds.Exclamation.Play();
                    }
                    catch (Exception ex) {
                        Trace.TraceError("Error exclamation play: {0}", ex); // Is this how it's done?
                    }
                } else { // Unix folk may decide for beep sound themselves
                    try {
                        System.Diagnostics.Process proc = new System.Diagnostics.Process();
                        proc.EnableRaisingEvents=false; 
                        proc.StartInfo.FileName = Program.Conf.SndPlayCmd;
                        proc.StartInfo.Arguments = Program.Conf.SndPlayPath;
                        proc.Start();
                    }
                    catch (Exception ex) {
                        Trace.TraceError("Error external UNIX play: {0}", ex); // Is this how it's done?
                    }
                }
            }
        }

        public void PopupSelf() {
            try {
                if (!InvokeRequired) {
                    var finalState = lastState;
                    var wasminimized = WindowState == FormWindowState.Minimized;
                    if (wasminimized) WindowState = FormWindowState.Maximized;
                    Show();
                    Activate();
                    Focus();
                    if (wasminimized) WindowState = finalState;
                }
                else InvokeFunc(PopupSelf);
            } catch (Exception ex) {
                Trace.TraceWarning("Error popping up self: {0}",ex.Message);
            }
        }

        /// <summary>
        /// Flashes window if its not foreground - until it is foreground
        /// </summary>
        protected void FlashWindow() {
            if (!Focused || !Visible || WindowState == FormWindowState.Minimized) {
                Visible = true;
                if (Environment.OSVersion.Platform != PlatformID.Unix) {
                    // todo implement for linux with #define NET_WM_STATE_DEMANDS_ATTENTION=42
                    var info = new WindowsApi.FLASHWINFO();
                    info.hwnd = Handle;
                    info.dwFlags = 0x0000000C | 0x00000003; // flash all until foreground
                    info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
                    WindowsApi.FlashWindowEx(ref info);
                }
            }
        }


        void UpdateDownloads() {
            try {
                if (Program.Downloader != null && !Program.CloseOnNext) {
                    // remove aborted
                    foreach (var pane in
                        new List<INotifyBar>(Program.NotifySection.Bars).OfType<DownloadBar>()
                                                                        .Where(x => x.Download.IsAborted || x.Download.IsComplete == true)) Program.NotifySection.RemoveBar(pane);

                    // update existing
                    foreach (var pane in new List<INotifyBar>(Program.NotifySection.Bars).OfType<DownloadBar>()) pane.UpdateInfo();
                }
            } catch (Exception ex) {
                Trace.TraceError("Error updating transfers: {0}", ex);
            }
        }

        void UpdateSystrayToolTip() {
            var sb = new StringBuilder();
            var bat = Program.TasClient.MyBattle;
            if (bat != null) {
                sb.AppendFormat("Players:{0}+{1}\n", bat.NonSpectatorCount, bat.SpectatorCount);
                sb.AppendFormat("Battle:{0}\n", bat.Founder);
            }
            else sb.AppendFormat("idle");
            var str = sb.ToString();
            systrayIcon.Text = str.Substring(0, Math.Min(str.Length, 64)); // tooltip only allows 64 characters
        }


        void Window_StateChanged(object sender, EventArgs e) {
            if (lastState != WindowState && WindowState == FormWindowState.Normal) SavePosition();
            if (WindowState != FormWindowState.Minimized) lastState = WindowState;
            else if (Program.Conf.MinimizeToTray) Visible = false;
        }

        void MainWindow_Load(object sender, EventArgs e) {
            if (Debugger.IsAttached) Text = "==== DEBUGGING ===";
            else Text = "Zero-K lobby";
            Text += " " + Assembly.GetEntryAssembly().GetName().Version;

            Icon = ZklResources.ZkIcon;
            systrayIcon.Icon = ZklResources.ZkIcon;

            Program.SpringScanner.Start();

            if (Program.Conf.StartMinimized) WindowState = FormWindowState.Minimized;
            else WindowState = Program.Conf.LastWindowState;

            if (Program.StartupArgs != null && Program.StartupArgs.Length > 0) navigationControl.Path = Program.StartupArgs[0];

            if (Program.Conf.ConnectOnStartup) Program.ConnectBar.TryToConnectTasClient();
            else NotifySection.AddBar(Program.ConnectBar);
        }

        void TorrentManager_DownloadAdded(object sender, EventArgs<Download> e) {
            Invoke(new Action(() => Program.NotifySection.AddBar(new DownloadBar(e.Data))));
        }

        void btnExit_Click(object sender, EventArgs e) {
            Exit();
        }

        void btnFriends_Click(object sender, EventArgs e) {
            if (frdWindow == null && FriendsWindow.Creatable) {
                frdWindow = new FriendsWindow();
                frdWindow.Show();
            }
            else frdWindow.Activate();
        }


        void systrayIcon_BalloonTipClicked(object sender, EventArgs e) {
            navigationControl.Path = baloonTipPath;
            PopupSelf();
        }


        void systrayIcon_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) PopupSelf();
        }


        void timer1_Tick(object sender, EventArgs e) {
            UpdateDownloads();
            UpdateSystrayToolTip();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) Program.CloseOnNext = true;
            if (Program.TasClient != null) Program.TasClient.Disconnect();
            Program.Conf.LastWindowState = WindowState;
            if (WindowState == FormWindowState.Normal) SavePosition();
            Program.SaveConfig();
            WindowState = FormWindowState.Minimized;
            systrayIcon.Visible = false;
            Hide();
        }
    }
}