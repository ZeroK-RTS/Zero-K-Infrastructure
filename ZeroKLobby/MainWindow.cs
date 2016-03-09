using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using PlasmaDownloader;
using SpringDownloader.Notifications;
using ZeroKLobby.Controls;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.Notifications;
using ZkData;

namespace ZeroKLobby
{
    public class MainWindow: ZklBaseForm
    {
        private readonly ToolStripMenuItem btnExit;

        private readonly NotifyIcon systrayIcon;
        private readonly Timer timer1 = new Timer();
        private readonly ContextMenuStrip trayStrip;

        private string baloonTipPath;

        private bool closeForReal;
        private FormWindowState lastState = FormWindowState.Normal;
        private readonly TableLayoutPanel tableLayoutPanel2;

        public MainWindow() {
            WindowState = FormWindowState.Maximized;
            tableLayoutPanel2 = new TableLayoutPanel();
            NotifySection = new NotifySection();
            navigationControl = new NavigationControl();
            tableLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(NotifySection, 0, 1);
            tableLayoutPanel2.Controls.Add(navigationControl, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle());
            // 
            // notifySection1
            // 
            NotifySection.AutoSize = true;
            NotifySection.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            NotifySection.BackColor = Color.Transparent;
            NotifySection.Dock = DockStyle.Fill;
            // 
            // navigationControl1
            // 
            navigationControl.BackColor = Color.Black;
            navigationControl.Dock = DockStyle.Fill;
            // 
            // MainWindow
            // 
            Controls.Add(tableLayoutPanel2);
            MinimumSize = new Size(1280,1024);


            FormClosing += MainWindow_FormClosing;
            Load += MainWindow_Load;
            SizeChanged += Window_StateChanged;
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            ResumeLayout(false);
            
            Instance = this;

            btnExit = new ToolStripMenuItem { Name = "btnExit", Size = new Size(92, 22), Text = "Exit" };
            btnExit.Click += btnExit_Click;

            trayStrip = new ContextMenuStrip();
            trayStrip.Items.AddRange(new ToolStripItem[] { btnExit });
            trayStrip.Name = "trayStrip";
            trayStrip.Size = new Size(93, 26);

            systrayIcon = new NotifyIcon { ContextMenuStrip = trayStrip, Text = "Zero-K", Visible = true };
            systrayIcon.MouseDown += systrayIcon_MouseDown;
            systrayIcon.BalloonTipClicked += systrayIcon_BalloonTipClicked;

            if (Program.Downloader != null)
            {
                timer1.Interval = 250;
                timer1.Tick += timer1_Tick;

                Program.Downloader.DownloadAdded += TorrentManager_DownloadAdded;
                timer1.Start();
            }
        }

        public NavigationControl navigationControl { get; }
        public ChatTab ChatTab { get { return navigationControl.ChatTab; } }
        public static MainWindow Instance { get; private set; }

        public NotifySection NotifySection { get; }


        public void DisplayLog() {
            if (!FormLog.Instance.Visible)
            {
                FormLog.Instance.Visible = true;
                FormLog.Instance.Focus();
            } else FormLog.Instance.Visible = false;
        }


        public void Exit() {
            if (closeForReal) return;
            closeForReal = true;
            Program.CloseOnNext = true;
            InvokeFunc(() => { systrayIcon.Visible = false; });
            InvokeFunc(Close);
        }

        public Control GetHoveredControl() {
            Control hovered;
            try
            {
                if (ActiveForm != null && ActiveForm.Visible && !(ActiveForm is ToolTipForm))
                {
                    hovered = ActiveForm.GetHoveredControl();
                    if (hovered != null) return hovered;
                }
                foreach (var lastForm in Application.OpenForms.OfType<Form>().Where(x => !(x is ToolTipForm) && x.Visible))
                {
                    hovered = lastForm.GetHoveredControl();
                    if (hovered != null) return hovered;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("MainWindow.GetHoveredControl error:", e);
                    //random crash with NULL error on line 140, is weird since already have NULL check (high probability in Linux when we changed focus)
            }
            return null;
        }

        public void InvokeFunc(Action funcToInvoke) {
            try
            {
                if (InvokeRequired) Invoke(funcToInvoke);
                else funcToInvoke();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error invoking: {0}", ex);
            }
        }

        /// <summary>
        ///     Alerts user
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

            if (isHidden || isPathDifferent)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    baloonTipPath = navigationPath;
                    if (showBalloon) systrayIcon.ShowBalloonTip(5000, "Zero-K", TextColor.StripCodes(message), ToolTipIcon.Info);
                }
            }
            if (isHidden && useFlashing) FlashWindow();
            if (isPathDifferent) navigationControl.HilitePath(navigationPath, useFlashing ? HiliteLevel.Flash : HiliteLevel.Bold);
            if (useSound)
            {
                if (Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    try
                    {
                        SystemSounds.Exclamation.Play();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error exclamation play: {0}", ex); // Is this how it's done?
                    }
                }
            }
        }

        public void PopupSelf() {
            try
            {
                if (!InvokeRequired)
                {
                    var finalState = lastState;
                    var wasminimized = WindowState == FormWindowState.Minimized;
                    if (wasminimized) WindowState = FormWindowState.Maximized;
                    Show();
                    Activate();
                    Focus();
                    if (wasminimized) WindowState = finalState;
                } else InvokeFunc(PopupSelf);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error popping up self: {0}", ex.Message);
            }
        }

        /// <summary>
        ///     Flashes window if its not foreground - until it is foreground
        /// </summary>
        protected void FlashWindow() {
            if (!Focused || !Visible || WindowState == FormWindowState.Minimized)
            {
                Visible = true;
                if (Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    // todo implement for linux with #define NET_WM_STATE_DEMANDS_ATTENTION=42
                    var info = new WindowsApi.FLASHWINFO();
                    info.hwnd = Handle;
                    info.dwFlags = 0x0000000C | 0x00000003; // flash all until foreground
                    info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
                    WindowsApi.FlashWindowEx(ref info);
                }
            }
        }


        private void UpdateDownloads() {
            try
            {
                if (Program.Downloader != null && !Program.CloseOnNext)
                {
                    // remove aborted
                    foreach (var pane in
                        new List<INotifyBar>(Program.NotifySection.Bars).OfType<DownloadBar>()
                            .Where(x => x.Download.IsAborted || x.Download.IsComplete == true)) Program.NotifySection.RemoveBar(pane);

                    // update existing
                    foreach (var pane in new List<INotifyBar>(Program.NotifySection.Bars).OfType<DownloadBar>()) pane.UpdateInfo();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error updating transfers: {0}", ex);
            }
        }

        private void UpdateSystrayToolTip() {
            var sb = new StringBuilder();
            var bat = Program.TasClient.MyBattle;
            if (bat != null)
            {
                sb.AppendFormat("Players:{0}+{1}\n", bat.NonSpectatorCount, bat.SpectatorCount);
                sb.AppendFormat("Battle:{0}\n", bat.Founder);
            } else sb.AppendFormat("idle");
            var str = sb.ToString();
            systrayIcon.Text = str.Substring(0, Math.Min(str.Length, 64)); // tooltip only allows 64 characters
        }


        private void Window_StateChanged(object sender, EventArgs e) {
            if (WindowState != FormWindowState.Minimized) lastState = WindowState;
        }

        private void MainWindow_Load(object sender, EventArgs e) {
            if (Debugger.IsAttached) Text = "==== DEBUGGING ===";
            else Text = "Zero-K lobby";
            Text += " " + Assembly.GetEntryAssembly()?.GetName().Version;

            Icon = ZklResources.ZkIcon;
            systrayIcon.Icon = ZklResources.ZkIcon;

            Program.SpringScanner.Start();

            if (Program.StartupArgs != null && Program.StartupArgs.Length > 0) navigationControl.Path = Program.StartupArgs[0];

            if (Program.Conf.ConnectOnStartup) Program.ConnectBar.TryToConnectTasClient();
            else NotifySection.AddBar(Program.ConnectBar);
        }

        private void TorrentManager_DownloadAdded(object sender, EventArgs<Download> e) {
            Invoke(new Action(() => Program.NotifySection.AddBar(new DownloadBar(e.Data))));
        }

        private void btnExit_Click(object sender, EventArgs e) {
            Exit();
        }


        private void systrayIcon_BalloonTipClicked(object sender, EventArgs e) {
            navigationControl.Path = baloonTipPath;
            PopupSelf();
        }


        private void systrayIcon_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) PopupSelf();
        }


        private void timer1_Tick(object sender, EventArgs e) {
            UpdateDownloads();
            UpdateSystrayToolTip();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e) {
            Program.CloseOnNext = true;
            if (Program.TasClient != null) Program.TasClient.RequestDisconnect();
            Program.SaveConfig();
            WindowState = FormWindowState.Minimized;
            systrayIcon.Visible = false;
            Hide();
        }

        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);
            if (FormBorderStyle == FormBorderStyle.None) TopMost = true;
        }

        protected override void OnDeactivate(EventArgs e) {
            base.OnDeactivate(e);
            TopMost = false;
        }
    }
}