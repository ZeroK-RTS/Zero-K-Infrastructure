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

        public MainWindow() {
            InitializeComponent();
            //Invalidate(true);
            Instance = this;
            //systrayIcon.BalloonTipClicked += systrayIcon_BalloonTipClicked;

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
            var lastForm = Application.OpenForms.OfType<Form>().LastOrDefault(x => !(x is ToolTipForm) && x.Visible);
            if (lastForm != null) {
                var hovered = lastForm.GetHoveredControl();
                if (hovered != null) return hovered;
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

            var isHidden = WindowState == FormWindowState.Minimized || Visible == false || WindowsApi.GetForegroundWindowHandle() != (int)Handle;
            var isPathDifferent = navigationControl.Path != navigationPath;

            if (isHidden || isPathDifferent) {
                if (!string.IsNullOrEmpty(message)) {
                    baloonTipPath = navigationPath;
                    if (showBalloon) systrayIcon.ShowBalloonTip(5000, "Zero-K", message, ToolTipIcon.Info);
                }
            }
            if (isHidden && useFlashing) FlashWindow();
            if (isPathDifferent) navigationControl.HilitePath(navigationPath, useFlashing ? HiliteLevel.Flash : HiliteLevel.Bold);
            if (useSound) SystemSounds.Exclamation.Play();
        }

        public void PopupSelf() {
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
        }

        /// <summary>
        /// Flashes window if its not foreground - until it is foreground
        /// </summary>
        protected void FlashWindow() {
            if (!Focused || !Visible || WindowState == FormWindowState.Minimized) {
                Visible = true;
                var info = new WindowsApi.FLASHWINFO();
                info.hwnd = Handle;
                info.dwFlags = 0x0000000C | 0x00000003; // flash all until foreground
                info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
                WindowsApi.FlashWindowEx(ref info);
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
            if (WindowState != FormWindowState.Minimized) lastState = WindowState;
            else if (Program.Conf.MinimizeToTray) Visible = false;
        }

        void MainWindow_Load(object sender, EventArgs e) {
            if (Debugger.IsAttached) Text = "==== DEBUGGING ===";
                // CONVERT else if (ApplicationDeployment.IsNetworkDeployed) Text = "Zero-K lobby " + ApplicationDeployment.CurrentDeployment.CurrentVersion;
            else Text += " not installed properly - update from http://zero-k.info/";
            Text += " " + Assembly.GetEntryAssembly().GetName().Version;

            Icon = ZklResources.ZkIcon;
            systrayIcon.Icon = ZklResources.ZkIcon;

            Program.SpringScanner.Start();

            if (Program.Conf.StartMinimized) WindowState = FormWindowState.Minimized;
            else WindowState = Program.Conf.LastWindowState;

            if (Program.StartupArgs != null && Program.StartupArgs.Length > 0) navigationControl.Path = Program.StartupArgs[0];
            else {
                // if first run show lobby start
                if (Program.Conf.IsFirstRun) navigationControl.Path = "http://zero-k.info/Wiki/LobbyStart";
            }

            // download primary game 
            var defaultTag = KnownGames.GetDefaultGame().RapidTag;
            if (!Program.Downloader.PackageDownloader.SelectedPackages.Contains(defaultTag)) {
                Program.Downloader.PackageDownloader.SelectPackage(defaultTag);
                if (Program.Downloader.PackageDownloader.GetByTag(defaultTag) != null) Program.Downloader.GetResource(DownloadType.MOD, defaultTag);
            }

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
            //hack ?if (e.CloseReason == CloseReason.UserClosing) Program.IsCrash = false;

            Program.CloseOnNext = true;
            if (Program.TasClient != null) Program.TasClient.Disconnect();
            Program.Conf.LastWindowState = WindowState;
            Program.SaveConfig();

        }
    }
}