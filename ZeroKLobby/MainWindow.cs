using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using PlasmaDownloader;
using SpringDownloader.Notifications;
using ZeroKLobby.Controls;
using ZeroKLobby.MainPages;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.MicroLobby.ExtrasTab;
using ZeroKLobby.Notifications;
using ZkData;

namespace ZeroKLobby
{
    public partial class MainWindow: Form
    {
        public enum MainPages
        {
            Home,
            SinglePlayer ,
            MultiPlayer,
            Skirmish
        }

        Mp3FileReader audioReader;

        string baloonTipPath;

        readonly ToolStripMenuItem btnExit;

        bool closeForReal;
        FormWindowState lastState = FormWindowState.Normal;
        readonly Dictionary<MainPages, Control> pages = new Dictionary<MainPages, Control>();
        Image resizeStoredBackground;

        readonly NotifyIcon systrayIcon;
        readonly Timer timer1 = new Timer();
        readonly ContextMenuStrip trayStrip;
        WaveOut waveOut;
        public ChatTab ChatTab
        {
            get { return navigator.ChatTab; }
        }
        public static MainWindow Instance { get; private set; }

        public NotifySection NotifySection
        {
            get { return notifySection1; }
        }
        public Navigator navigationControl
        {
            get { return navigator; }
        }

        Navigator navigator;


        public MainWindow()
        {
            InitializeComponent();
            SuspendLayout();
            SetStyle( ControlStyles.DoubleBuffer, true);

            pages[MainPages.Home] = new HomePage();
            pages[MainPages.SinglePlayer] = new SinglePlayerPage();
            pages[MainPages.Skirmish] = new BattleListTab();

            Instance = this;

            btnExit = new ToolStripMenuItem { Name = "btnExit", Size = new Size(92, 22), Text = "Exit" };
            btnExit.Click += btnExit_Click;

            trayStrip = new ContextMenuStrip();
            trayStrip.Items.AddRange(new ToolStripItem[] { btnExit });
            trayStrip.Name = "trayStrip";
            trayStrip.Size = new Size(93, 26);

            systrayIcon = new NotifyIcon { ContextMenuStrip = trayStrip, Text = "Zero-K Launcher", Visible = true };
            systrayIcon.MouseDown += systrayIcon_MouseDown;
            systrayIcon.BalloonTipClicked += systrayIcon_BalloonTipClicked;

            if (Program.Downloader != null) {
                timer1.Interval = 250;
                timer1.Tick += timer1_Tick;

                Program.Downloader.DownloadAdded += TorrentManager_DownloadAdded;
                timer1.Start();
            }

            var home = new HomePage();
            switchPanel1.SwitchContent(home);

            navigator = new Navigator(navigationControl1, flowLayoutPanel1);

            //btnWindowed_Click(this, EventArgs.Empty);
            ResumeLayout();
        }


        public void DisplayLog()
        {
            if (!FormLog.Instance.Visible) {
                FormLog.Instance.Visible = true;
                FormLog.Instance.Focus();
            } else FormLog.Instance.Visible = false;
        }


        public void Exit()
        {
            if (closeForReal) return;
            closeForReal = true;
            Program.CloseOnNext = true;
            InvokeFunc(() => { systrayIcon.Visible = false; });
            InvokeFunc(Close);
        }

        public Control GetHoveredControl()
        {
            Control hovered;
            try {
                if (ActiveForm != null && ActiveForm.Visible && !(ActiveForm is ToolTipForm)) {
                    hovered = ActiveForm.GetHoveredControl();
                    if (hovered != null) return hovered;
                }
                foreach (Form lastForm in Application.OpenForms.OfType<Form>().Where(x => !(x is ToolTipForm) && x.Visible)) {
                    hovered = lastForm.GetHoveredControl();
                    if (hovered != null) return hovered;
                }
            } catch (Exception e) {
                Trace.TraceError("MainWindow.GetHoveredControl error:", e);
                    //random crash with NULL error on line 140, is weird since already have NULL check (high probability in Linux when we changed focus)
            }
            return null;
        }

        public void InvokeFunc(Action funcToInvoke)
        {
            try {
                if (InvokeRequired) Invoke(funcToInvoke);
                else funcToInvoke();
            } catch (Exception ex) {
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
        public void NotifyUser(string navigationPath, string message, bool useSound = false, bool useFlashing = false)
        {
            bool showBalloon =
                !((Program.Conf.DisableChannelBubble && navigationPath.Contains("chat/channel/")) ||
                  (Program.Conf.DisablePmBubble && navigationPath.Contains("chat/user/")));

            bool isHidden = WindowState == FormWindowState.Minimized || Visible == false || ActiveForm == null;
            bool isPathDifferent = navigationControl.Path != navigationPath;

            if (isHidden || isPathDifferent) {
                if (!string.IsNullOrEmpty(message)) {
                    baloonTipPath = navigationPath;
                    if (showBalloon) systrayIcon.ShowBalloonTip(5000, "Zero-K", TextColor.StripCodes(message), ToolTipIcon.Info);
                }
            }
            if (isHidden && useFlashing) FlashWindow();
            if (isPathDifferent) navigationControl.HilitePath(navigationPath, useFlashing ? HiliteLevel.Flash : HiliteLevel.Bold);
            if (useSound) {
                try {
                    SystemSounds.Exclamation.Play();
                } catch (Exception ex) {
                    Trace.TraceError("Error exclamation play: {0}", ex); // Is this how it's done?
                }
            }
        }

        public void PopupSelf()
        {
            try {
                if (!InvokeRequired) {
                    FormWindowState finalState = lastState;
                    bool wasminimized = WindowState == FormWindowState.Minimized;
                    if (wasminimized) WindowState = FormWindowState.Maximized;
                    Show();
                    Activate();
                    Focus();
                    if (wasminimized) WindowState = finalState;
                } else InvokeFunc(PopupSelf);
            } catch (Exception ex) {
                Trace.TraceWarning("Error popping up self: {0}", ex.Message);
            }
        }

        public Task SwitchPage(MainPages page, bool animate = true)
        {
            return switchPanel1.SwitchContent(pages[page], animate ? SwitchPanel.AnimType.SlideLeft : (SwitchPanel.AnimType?)null);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (FormBorderStyle == FormBorderStyle.None) TopMost = true;
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            TopMost = false;
        }


        /// <summary>
        ///     Flashes window if its not foreground - until it is foreground
        /// </summary>
        protected void FlashWindow()
        {
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


        void UpdateDownloads()
        {
            try {
                if (Program.Downloader != null && !Program.CloseOnNext) {
                    // remove aborted
                    foreach (DownloadBar pane in
                        new List<INotifyBar>(Program.NotifySection.Bars).OfType<DownloadBar>()
                            .Where(x => x.Download.IsAborted || x.Download.IsComplete == true)) Program.NotifySection.RemoveBar(pane);

                    // update existing
                    foreach (DownloadBar pane in new List<INotifyBar>(Program.NotifySection.Bars).OfType<DownloadBar>()) pane.UpdateInfo();
                }
            } catch (Exception ex) {
                Trace.TraceError("Error updating transfers: {0}", ex);
            }
        }

        void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.CloseOnNext = true;
            if (Program.TasClient != null) Program.TasClient.RequestDisconnect();
            Program.SaveConfig();
            WindowState = FormWindowState.Minimized;
            systrayIcon.Visible = false;
            Hide();
        }


        void MainWindow_Load(object sender, EventArgs e)
        {
            if (Debugger.IsAttached) Text = "==== DEBUGGING ===";
            else Text = "Zero-K launcher";
            Text += " " + Assembly.GetEntryAssembly().GetName().Version;

            Icon = ZklResources.ZkIcon;
            systrayIcon.Icon = ZklResources.ZkIcon;

            Program.SpringScanner.Start();

            if (Program.StartupArgs != null && Program.StartupArgs.Length > 0) navigationControl.Path = Program.StartupArgs[0];

            if (Program.Conf.ConnectOnStartup) Program.ConnectBar.TryToConnectTasClient();
            else NotifySection.AddBar(Program.ConnectBar);

            if (Environment.OSVersion.Platform != PlatformID.Unix) {
                waveOut = new WaveOut();
                audioReader = new Mp3FileReader(new MemoryStream(Sounds.menu_music_ROM));
                waveOut.Init(audioReader);
                waveOut.Play();
            }
        }

        void TorrentManager_DownloadAdded(object sender, EventArgs<Download> e)
        {
            Invoke(new Action(() => Program.NotifySection.AddBar(new DownloadBar(e.Data))));
        }

        void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized) lastState = WindowState;
        }

        void btnExit_Click(object sender, EventArgs e)
        {
            Exit();
        }

        void btnSnd_Click(object sender, EventArgs e)
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix) {
                if (waveOut.PlaybackState == PlaybackState.Playing) waveOut.Stop();
                else {
                    audioReader.Position = 0;
                    waveOut.Play();
                }
            }
        }

        void btnWindowed_Click(object sender, EventArgs e)
        {
            Image image = BackgroundImage;
            BackgroundImage = null;
            if (FormBorderStyle == FormBorderStyle.None) {
                TopMost = false;
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.Sizable;
            } else {
                FormBorderStyle = FormBorderStyle.None;
                TopMost = true;
                WindowState = FormWindowState.Maximized;
            }
            BackgroundImage = image;
        }


        void systrayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            navigationControl.Path = baloonTipPath;
            PopupSelf();
        }


        void systrayIcon_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) PopupSelf();
        }


        void timer1_Tick(object sender, EventArgs e)
        {
            UpdateDownloads();
        }

        private void bitmapButton1_Click(object sender, EventArgs e)
        {

        }

        private void panelRight_Paint(object sender, PaintEventArgs e)
        {
            if (panelRight.BackgroundImage == null) {
                var img = new Bitmap(panelRight.Width, panelRight.Height);
                using (var g = Graphics.FromImage(img)) {
                    g.TranslateTransform(-panelRight.Left, -panelRight.Top);
                    InvokePaintBackground(this, new PaintEventArgs(g, panelRight.Bounds));
                    g.TranslateTransform(panelRight.Left, panelRight.Top);

                    //g.DrawImage(BackgroundImage, panelRight.ClientRectangle, panelRight.Left, panelRight.Top, panelRight.Width, panelRight.Height, GraphicsUnit.Pixel);
                    FrameBorderRenderer.Instance.RenderToGraphics(g, panelRight.ClientRectangle, FrameBorderRenderer.StyleType.Shraka);
                }
                panelRight.BackgroundImageLayout = ImageLayout.None;
                panelRight.BackgroundImage = img;    
            }

            

        }

        private void btnHide_Click(object sender, EventArgs e)
        {
            panelRight.Visible = false;
        }

        private void panelRight_SizeChanged(object sender, EventArgs e)
        {
            panelRight.BackgroundImage = null;
        }

    }
}