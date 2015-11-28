using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using LobbyClient;
using NAudio.Wave;
using PlasmaDownloader;
using SpringDownloader.Notifications;
using ZeroKLobby.BattleRoom;
using ZeroKLobby.Controls;
using ZeroKLobby.MainPages;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.MicroLobby.ExtrasTab;
using ZeroKLobby.Notifications;
using ZkData;

namespace ZeroKLobby
{
    public partial class MainWindow : Form
    {
        public enum MainPages
        {
            Home,
            SinglePlayer,
            MultiPlayer,
            Skirmish,
            CustomBattles,
            BattleRoom
        }

        Mp3FileReader audioReader;

        string baloonTipPath;

        ToolStripMenuItem btnExit;

        bool closeForReal;
        FormWindowState lastState = FormWindowState.Normal;
        readonly Dictionary<MainPages, Control> pages = new Dictionary<MainPages, Control>();

        NotifyIcon systrayIcon;
        readonly Timer timer1 = new Timer();
        ContextMenuStrip trayStrip;
        DirectSoundOut waveOut;
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
            Font = Config.GeneralFont;
            Instance = this;
            InitializeComponent();
            SuspendLayout();
            SetStyle(ControlStyles.DoubleBuffer, true);

            btnBack.Image = Buttons.left.GetResized(40, 40);
            btnHide.Image = Buttons.down.GetResized(32, 32);

            lbMainPageTitle.Font = Config.MenuFont;
            lbRightPanelTitle.Font = Config.MenuFont;

            if (this.IsInDesignMode()) return;

            SetupMainPages();
            SetupSystray();
            navigator = new Navigator(navigationControl1, flowLayoutPanel1);

            if (Program.Downloader != null)
            {
                timer1.Interval = 250;
                timer1.Tick += timer1_Tick;

                Program.Downloader.DownloadAdded += TorrentManager_DownloadAdded;
                timer1.Start();
            }


            ResumeLayout();

            Spring.AnySpringStarted += (sender, args) => { if (waveOut != null) waveOut.Stop(); };

            btnWindowed_Click(this, EventArgs.Empty); // switch to fullscreen
        }

        void MainWindow_Load(object sender, EventArgs e)
        {
            if (Debugger.IsAttached) Text = "==== DEBUGGING ====";
            else Text = "Zero-K launcher";
            Text += " " + Assembly.GetEntryAssembly().GetName().Version;

            Icon = ZklResources.ZkIcon;
            systrayIcon.Icon = ZklResources.ZkIcon;

            Program.SpringScanner.Start();

            if (Program.StartupArgs != null && Program.StartupArgs.Length > 0) navigationControl.Path = Program.StartupArgs[0];

            connectBar.Init(Program.TasClient);
            if (Program.Conf.ConnectOnStartup) connectBar.TryToConnectTasClient();

            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                waveOut = new DirectSoundOut();
                audioReader = new Mp3FileReader(new MemoryStream(Sounds.menu_music_ROM));
                waveOut.Init(audioReader);

                btnSnd_Click(this, EventArgs.Empty);
            }
        }

        public BattleRoomPage BattleRoomPage
        {
            get { return (BattleRoomPage)pages[MainPages.BattleRoom]; }
        }


        void SetupMainPages()
        {
            pages[MainPages.Home] = new HomePage();
            pages[MainPages.SinglePlayer] = new SinglePlayerPage();
            pages[MainPages.Skirmish] = new SkirmishControl();
            pages[MainPages.MultiPlayer] = new MultiPlayerPage();
            pages[MainPages.CustomBattles] = new BattleListTab();
            pages[MainPages.BattleRoom] = new BattleRoomPage();

            foreach (var c in pages.Values) switchPanel1.SetupTabPage(c);
            SwitchPage(MainPages.Home, false);
        }

        void SetupSystray()
        {
            btnExit = new ToolStripMenuItem { Name = "btnExit", Size = new Size(92, 22), Text = "Exit" };
            btnExit.Click += btnExit_Click;

            trayStrip = new ContextMenuStrip();
            trayStrip.Items.AddRange(new ToolStripItem[] { btnExit });
            trayStrip.Name = "trayStrip";
            trayStrip.Size = new Size(93, 26);

            systrayIcon = new NotifyIcon { ContextMenuStrip = trayStrip, Text = "Zero-K Launcher", Visible = true };
            systrayIcon.MouseDown += systrayIcon_MouseDown;
            systrayIcon.BalloonTipClicked += systrayIcon_BalloonTipClicked;
        }


        public void DisplayLog()
        {
            if (!FormLog.Instance.Visible)
            {
                FormLog.Instance.Visible = true;
                FormLog.Instance.Focus();
            }
            else FormLog.Instance.Visible = false;
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
            try
            {
                if (ActiveForm != null && ActiveForm.Visible && !(ActiveForm is ToolTipForm))
                {
                    hovered = ActiveForm.GetHoveredControl();
                    if (hovered != null) return hovered;
                }
                foreach (Form lastForm in Application.OpenForms.OfType<Form>().Where(x => !(x is ToolTipForm) && x.Visible))
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

        public void InvokeFunc(Action funcToInvoke)
        {
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
        public void NotifyUser(string navigationPath, string message, bool useSound = false, bool useFlashing = false)
        {
            bool showBalloon =
                !((Program.Conf.DisableChannelBubble && navigationPath.Contains("chat/channel/")) ||
                  (Program.Conf.DisablePmBubble && navigationPath.Contains("chat/user/")));

            bool isHidden = WindowState == FormWindowState.Minimized || Visible == false || ActiveForm == null;
            bool isPathDifferent = navigationControl.Path != navigationPath;

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

        public void PopupSelf()
        {
            try
            {
                if (!InvokeRequired)
                {
                    FormWindowState finalState = lastState;
                    bool wasminimized = WindowState == FormWindowState.Minimized;
                    if (wasminimized) WindowState = FormWindowState.Maximized;
                    Show();
                    Activate();
                    Focus();
                    if (wasminimized) WindowState = finalState;
                }
                else InvokeFunc(PopupSelf);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error popping up self: {0}", ex.Message);
            }
        }


        Image lastBgImage;
        public async Task SwitchPage(MainPages page, bool animate = true)
        {
            var target = pages[page];
            var ipage = (IMainPage)target;
            if (ipage != null)
            {
                lbMainPageTitle.Text = ipage.Title;
                if (page == MainPages.Home) btnBack.Visible = false;
                else btnBack.Visible = true;
            }
            else
            {
                btnBack.Visible = false;
                lbMainPageTitle.Text = "";
            }

            await switchPanel1.SwitchContent(target, animate ? SwitchPanel.AnimType.SlideLeft : (SwitchPanel.AnimType?)null);
            if (ipage != null && lastBgImage != ipage.MainWindowBgImage)
            {
                lastBgImage = ipage.MainWindowBgImage;
                BackgroundImage = null;
                panelRight.BackgroundImage = null;
            }

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


        void UpdateDownloads()
        {
            try
            {
                if (Program.Downloader != null && !Program.CloseOnNext)
                {
                    // remove aborted
                    foreach (DownloadBar pane in
                        new List<INotifyBar>(Program.NotifySection.Bars).OfType<DownloadBar>()
                            .Where(x => x.Download.IsAborted || x.Download.IsComplete == true)) Program.NotifySection.RemoveBar(pane);

                    // update existing
                    foreach (DownloadBar pane in new List<INotifyBar>(Program.NotifySection.Bars).OfType<DownloadBar>()) pane.UpdateInfo();
                }
            }
            catch (Exception ex)
            {
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
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    waveOut.Stop();
                    btnSnd.Image = Buttons.soundOff.GetResizedWithCache(32, 32);
                }
                else
                {
                    audioReader.Position = 0;
                    waveOut.Play();
                    btnSnd.Image = Buttons.soundOn.GetResizedWithCache(32, 32);
                }
            }
        }

        void btnWindowed_Click(object sender, EventArgs e)
        {
            if (FormBorderStyle == FormBorderStyle.None)
            {
                TopMost = false;
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.Sizable;
                btnWindowed.Image = Buttons.win_max.GetResizedWithCache(32, 32);
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
                TopMost = true;
                WindowState = FormWindowState.Maximized;
                btnWindowed.Image = Buttons.win_min.GetResizedWithCache(32, 32);
            }
        }


        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (BackgroundImage == null)
            {
                var target = (IMainPage)switchPanel1.CurrentTarget;
                if (target != null && target.MainWindowBgImage != null)
                {
                    BackgroundImage = target.MainWindowBgImage.GetResized(ClientRectangle.Width, ClientRectangle.Height);
                }
            }
            base.OnPaintBackground(e);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            BackgroundImage = null;
            panelRight.BackgroundImage = null;
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



        private void btnHide_Click(object sender, EventArgs e)
        {
            panelRight.Visible = false;
            Program.MainWindow.navigationControl.Path = "";
        }


        private void btnBack_Click(object sender, EventArgs e)
        {
            var page = switchPanel1.CurrentTarget as IMainPage;
            if (page != null) page.GoBack();
        }

    }
}