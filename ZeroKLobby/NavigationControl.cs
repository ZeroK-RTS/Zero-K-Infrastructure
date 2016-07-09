using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby.Controls;
using ZeroKLobby.MapDownloader;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.MicroLobby.ExtrasTab;
using ZkData;

namespace ZeroKLobby
{
    public class NavigationControl: ZklBaseControl
    {
        private const int TabButtonHeight = 70;
        public const int TopRightMiniIconSize = 32;
        private const int TopRightSpace = 200;
        private readonly Stack<NavigationStep> backStack = new Stack<NavigationStep>();
        private readonly Stack<NavigationStep> forwardStack = new Stack<NavigationStep>();
        private readonly Timer isBusyTimer = new Timer();
        private readonly Dictionary<INavigatable, string> lastTabPaths = new Dictionary<INavigatable, string>();
        private readonly int systemDoubleClickTime = SystemInformation.DoubleClickTime*10000;

        public event Action<string> PageChanged = s => { };

        public readonly HeadlessTabControl tabControl;
        private ZklTextBox urlBox;

        private NavigationStep _currentPage;

        private int clickCount;
        private long lastClick;
        private FlowLayoutPanel flowLayoutPanel;
        private TableLayoutPanel table;

        public BitmapButton sndButton;

        public NavigationControl() {
            SuspendLayout();
            
            BorderStyle = BorderStyle.None;
            flowLayoutPanel = new FlowLayoutPanel();
            tabControl = new HeadlessTabControl();

            table = new TableLayoutPanel();
            table.RowCount = 1;
            table.ColumnCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TopRightSpace));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.BackColor = Color.Transparent;
            table.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Controls.Add(table);

            var miniIconPanel = new FlowLayoutPanel()
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
            };
            var versionLabel = new Label()
            {
                Text = "Zero-K Lobby " + System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version,
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Right,
                AutoSize = true,
            };
            // contains the miniIconPanel and versionLabel
            var rightHolderPanel = new TableLayoutPanel()
            {
                RowCount = 2,
                ColumnCount = 1,
                Dock = DockStyle.Right,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };
            rightHolderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            rightHolderPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightHolderPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            
            rightHolderPanel.Controls.Add(miniIconPanel);
            rightHolderPanel.Controls.Add(versionLabel);
            table.Controls.Add(rightHolderPanel, 1, 0);

            flowLayoutPanel.AutoScroll = false;
            flowLayoutPanel.AutoSize = true;
            flowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanel.BackColor = Color.Transparent;
            flowLayoutPanel.Dock = DockStyle.Top;
            flowLayoutPanel.Padding = new Padding(13);
            flowLayoutPanel.WrapContents = false;
    
            table.Controls.Add(flowLayoutPanel, 0, 0);
    
            Margin = new Padding(0);
            Name = "NavigationControl";
            Size = new Size(703, 219);

            isBusyTimer.Interval = 120; //timer tick to update "isBusyIcon" every 120 ms.
            isBusyTimer.Tick += (sender, args) => { Application.UseWaitCursor = CurrentNavigatable?.IsBusy == true; };
            isBusyTimer.Start();

            Instance = this;

            SetupTabButtons(flowLayoutPanel);
            CreateTopRightMiniIcons(miniIconPanel);
            PerformLayout();
            ResumeLayout(false);

            tabControl.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            tabControl.Top = flowLayoutPanel.Bottom+10;
            tabControl.Left = 10;
            tabControl.Width = Width-20;
            tabControl.Height = Height - flowLayoutPanel.Height-20;

            Controls.Add(tabControl);

            InitializeTabPageContent();
        }

        private void CreateTopRightMiniIcons(FlowLayoutPanel miniIconPanel) {
            urlBox = new ZklTextBox
            {
                Size = new Size(110, 20),
                TabIndex = 1,
                Font = Config.GeneralFontSmall,
                Margin = new Padding(10)
            };
            urlBox.KeyDown += urlBox_KeyDown;
            urlBox.MouseDown += urlBox_MouseDown;

            var minMaxButton = new BitmapButton
            {
                ButtonStyle = FrameBorderRenderer.StyleType.IconOnly,
                SoundType = SoundPalette.SoundType.Click,
                Height = TopRightMiniIconSize,
                Width = TopRightMiniIconSize,
                Image = Buttons.win_max.GetResizedWithCache(TopRightMiniIconSize, TopRightMiniIconSize)
            };
            minMaxButton.Click += (sender, args) => Program.MainWindow?.SwitchFullscreenState();

            var exitButton = new BitmapButton()
            {
                ButtonStyle = FrameBorderRenderer.StyleType.IconOnly,
                SoundType = SoundPalette.SoundType.Click,
                Height = TopRightMiniIconSize,
                Width = TopRightMiniIconSize,
                Image = Buttons.exit.GetResizedWithCache(TopRightMiniIconSize, TopRightMiniIconSize),
            };
            exitButton.Click += (sender, args) => Program.MainWindow?.Exit();

            var backButton = new BitmapButton()
            {
                ButtonStyle = FrameBorderRenderer.StyleType.IconOnly,
                SoundType = SoundPalette.SoundType.Click,
                Height = TopRightMiniIconSize,
                Width = TopRightMiniIconSize,
                Image = Buttons.left.GetResizedWithCache(TopRightMiniIconSize, TopRightMiniIconSize),
            };
            backButton.Click += (sender, args) => { NavigateBack(); };

            var forwardImage = Buttons.left.GetResizedWithCache(TopRightMiniIconSize, TopRightMiniIconSize);
            forwardImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
            var forwardButton = new BitmapButton()
            {
                ButtonStyle = FrameBorderRenderer.StyleType.IconOnly,
                SoundType = SoundPalette.SoundType.Click,
                Height = TopRightMiniIconSize,
                Width = TopRightMiniIconSize,
                Image = forwardImage,
            };
            forwardButton.Click += (sender, args) => { NavigateForward(); };

            sndButton = new BitmapButton()
            {
                ButtonStyle = FrameBorderRenderer.StyleType.IconOnly,
                SoundType = SoundPalette.SoundType.Click,
                Height = TopRightMiniIconSize,
                Width = TopRightMiniIconSize,
                Image = Buttons.soundOn.GetResizedWithCache(TopRightMiniIconSize, TopRightMiniIconSize),
            };
            sndButton.Click += (sender, args) => Program.MainWindow?.SwitchMusicOnOff();

            var settingsButton = new BitmapButton()
            {
                ButtonStyle = FrameBorderRenderer.StyleType.IconOnly,
                SoundType = SoundPalette.SoundType.Click,
                Height = TopRightMiniIconSize,
                Width = TopRightMiniIconSize,
                Image = Buttons.settings.GetResizedWithCache(TopRightMiniIconSize, TopRightMiniIconSize),
            };
            settingsButton.Click += (sender, args) => { Path = "settings"; };

            Program.ToolTip.SetText(exitButton,"Exit");
            Program.ToolTip.SetText(minMaxButton, "Fullscreen on/off");
            Program.ToolTip.SetText(settingsButton, "Settings");
            Program.ToolTip.SetText(sndButton, "Music on/off");
            Program.ToolTip.SetText(forwardButton, "Forward");
            Program.ToolTip.SetText(backButton, "Back");

            miniIconPanel.Controls.Add(exitButton);
            miniIconPanel.Controls.Add(minMaxButton);
            miniIconPanel.Controls.Add(settingsButton);
            miniIconPanel.Controls.Add(sndButton);
            miniIconPanel.Controls.Add(urlBox);
            miniIconPanel.Controls.Add(forwardButton);
            miniIconPanel.Controls.Add(backButton);
        }

        private static void SetupTabButtons(Control control) {
            ButtonList = new List<ButtonInfo> //normal arrangement
            {
                new ButtonInfo
                {
                    Label = "MISSION",
                    TargetPath = string.Format("{0}/Missions?no_menu=1", GlobalConst.BaseSiteUrl),
                    Icon = Buttons.sp,
                    Width = 200,
                    Height = TabButtonHeight
                },
                /*new ButtonInfo
                {
                    Label = "SKIRMISH",
                    TargetPath = "skirmish",
                    Icon = Buttons.sp,
                    Width = 200,
                    Height = TabButtonHeight
                },*/
                new ButtonInfo { Label = "MULTIPLAYER", TargetPath = "battles", Icon = Buttons.mp, Width = 250, Height = TabButtonHeight },
                new ButtonInfo { Label = "WEB", TargetPath = GlobalConst.BaseSiteUrl + "/", Icon = Buttons.extras, Height = TabButtonHeight, Width = 150 },
                new ButtonInfo { Label = "CHAT", TargetPath = "chat", Icon = Buttons.chat, Height = TabButtonHeight, Width = 150 }
         
            };
            foreach (var but in ButtonList) control.Controls.Add(but.GetButton());
        }

        private static List<ButtonInfo> ButtonList { get; set; }
        private bool CanGoBack { get { return backStack.Any(); } }
        private bool CanGoForward { get { return forwardStack.Any(); } }


        private NavigationStep CurrentPage
        {
            get { return _currentPage; }
            set
            {
                _currentPage = value;
                urlBox.Text = Path;

                ButtonList.ForEach(x => x.IsSelected = false); //unselect all button

                var selbut = ButtonList.Where(x => Path.StartsWith(x.TargetPath)).OrderByDescending(x => x.TargetPath.Length).FirstOrDefault();
                if (selbut != null)
                {
                    selbut.IsSelected = true;
                    selbut.IsAlerting = false;
                }

                var navigable =
                    tabControl.Controls.OfType<object>()
                        .Select(GetINavigatableFromControl)
                        .FirstOrDefault(x => x != null && Path.StartsWith(x.PathHead)); //find TAB with correct PathHead
                if (navigable != null) navigable.Hilite(HiliteLevel.None, Path); //cancel hilite ChatTab's tab (if meet some condition)
            }
        }
        public ChatTab ChatTab { get; private set; }
        public static NavigationControl Instance { get; private set; }

        public string Path
        {
            get { return CurrentPage != null ? CurrentPage.ToString() : string.Empty; }
            set
            {
                if (value.ToLower().StartsWith("zk://")) value = value.Substring(5);

                var parts = value.Split('@');
                for (var i = 1; i < parts.Length; i++)
                {
                    var action = parts[i];
                    ActionHandler.PerformAction(action);
                }
                value = parts[0];

                if (CurrentPage != null && CurrentPage.ToString() == value) return; // we are already there, no navigation needed

                if (value.StartsWith("www.")) value = "http://" + value;
                var step = GoToPage(value.Split('/')); //go to page
                if (step != null)
                {
                    if (CurrentPage != null && CurrentPage.ToString() != value) backStack.Push(CurrentPage);
                    CurrentPage = step;
                } else if (value.StartsWith("http://") || value.StartsWith("https://") || value.StartsWith("file://")) Program.BrowserInterop.OpenUrl(value); //this open external browser

                PageChanged?.Invoke(Path);
            }
        }

        public INavigatable CurrentNavigatable { get { return tabControl.SelectedTab.Controls.OfType<INavigatable>().FirstOrDefault(); } }

        private void InitializeTabPageContent() {
            tabControl.TabPages.Clear();
            ChatTab = new ChatTab();
            lastTabPaths[ChatTab] = "chat/channel/zk";
            AddTabPage(ChatTab, "Chat");
            if (Environment.OSVersion.Platform != PlatformID.Unix && !Program.Conf.UseExternalBrowser)
            {
                if (!Program.Conf.SingleInstance) //run in multiple TAB?
                {
                    AddTabPage(new BrowserTab(GlobalConst.BaseSiteUrl + "/Maps", false), "Maps");
                    AddTabPage(new BrowserTab(GlobalConst.BaseSiteUrl + "/Missions", true, GlobalConst.BaseSiteUrl + "/Missions?no_menu=1"), "sp");
                    AddTabPage(new BrowserTab(GlobalConst.BaseSiteUrl + "/Battles", false), "rp");
                    AddTabPage(new BrowserTab(GlobalConst.BaseSiteUrl + "/Planetwars", false), "pw");
                    AddTabPage(new BrowserTab(GlobalConst.BaseSiteUrl + "/Forum", false), "fm");
                }
                AddTabPage(new BrowserTab(GlobalConst.BaseSiteUrl, true), "hm");
            }
            var battles = new BattleListTab();
            AddTabPage(battles, "Battles");
            AddTabPage(new SettingsTab(), "Settings");
            AddTabPage(new ServerTab(), "Server");
            AddTabPage(new DownloaderTab(), "Rapid");
            AddTabPage(new SkirmishControl(), "Skirmish");
            var home = AddTabPage(new WelcomeTab(), "Welcome");

            tabControl.SelectTab(home);
        }

        public INavigatable GetInavigatableByPath(string path) {
            //get which TAB has which PathHead (header)
            foreach (TabPage tabPage in tabControl.Controls)
            {
                var navigatable = GetINavigatableFromControl(tabPage);
                if (path.Contains(navigatable.PathHead)) return navigatable;
            }
            return null;
        }


        public bool HilitePath(string navigationPath, HiliteLevel hiliteLevel) {
            if (string.IsNullOrEmpty(navigationPath)) return false;
            if (hiliteLevel == HiliteLevel.Flash) foreach (var b in ButtonList) if (navigationPath.StartsWith(b.TargetPath)) b.IsAlerting = true; //make BUTTON turn red

            var navigable =
                tabControl.Controls.OfType<object>().Select(GetINavigatableFromControl).First(x => x != null && navigationPath.Contains(x.PathHead));
            if (navigable != null) return navigable.Hilite(hiliteLevel, navigationPath); //make ChatTab's tab to flash
            return false;
        }

        public void NavigateBack() {
            if (CanGoBack) GoBack();
        }

        public void NavigateForward() {
            if (CanGoForward) GoForward();
        }

        public void SwitchTab(string targetPath) {
            //called by ButtonInfo.cs when clicked. "targetPath" is usually a "PathHead"
            foreach (TabPage tabPage in tabControl.Controls)
            {
                var nav = GetINavigatableFromControl(tabPage);
                if (nav.PathHead == targetPath)
                {
                    if (CurrentNavigatable == nav) Path = targetPath; // double click on forum go to forum home
                    else
                    {
                        string lastPath;
                        if (lastTabPaths.TryGetValue(nav, out lastPath)) targetPath = lastPath; //go to current page of the tab
                        Path = targetPath;
                    }
                    return;
                }
            }
            Path = targetPath;
        }


        private TabPage AddTabPage(Control content, string name = null) {
            name = name ?? content.Text ?? content.Name;
            var tb = new TabPage(name);
            tb.Dock = DockStyle.Fill;
            tb.Controls.Add(content);
            content.Dock = DockStyle.Fill;
            tabControl.TabPages.Add(tb);
            return tb;
        }

        private INavigatable GetINavigatableFromControl(object obj) {
            if (obj is TabPage) obj = ((TabPage)obj).Controls.OfType<Control>().FirstOrDefault();
            return obj as INavigatable;
        }


        private void GoBack() {
            if (forwardStack.Count == 0 || forwardStack.Peek().ToString() != CurrentPage.ToString()) forwardStack.Push(CurrentPage);
            CurrentPage = backStack.Pop();
            GoToPage(CurrentPage.Path);
        }

        private void GoForward() {
            if (backStack.Count == 0 || backStack.Peek().ToString() != CurrentPage.ToString()) backStack.Push(CurrentPage);
            CurrentPage = forwardStack.Pop();
            GoToPage(CurrentPage.Path);
        }


        private NavigationStep GoToPage(string[] path) // todo cleanup
        {
            foreach (TabPage tabPage in tabControl.Controls)
            {
                var navigatable = GetINavigatableFromControl(tabPage); //translate tab button into the page it represent
                if (navigatable != null && navigatable.TryNavigate(path))
                {
                    tabControl.SelectTab(tabPage);
                    lastTabPaths[navigatable] = string.Join("/", path);
                    return new NavigationStep { Path = path };
                }
            }
            return null;
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            //e.Graphics.Clear(Color.FromArgb(255, 10, 30, 40));
            using (var lb = new LinearGradientBrush(table.Bounds, Color.FromArgb(255, 19, 65, 73), Color.FromArgb(255, 10, 30, 40), 90)) 
                e.Graphics.FillRectangle(lb, table.Bounds);

            var rect = new Rectangle(0, table.Bounds.Height, Width, Height - table.Bounds.Height);
            using (var lb = new LinearGradientBrush(new Rectangle(rect.X, rect.Y - 1, rect.Width, rect.Height + 1), Color.FromArgb(255, 10, 30, 40), Color.FromArgb(255, 0, 0, 0), 90))
                e.Graphics.FillRectangle(lb, rect);
        }



        private void urlBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyData == Keys.Return)
            {
                NavigateToUrlBoxText();
                e.Handled = true;
            }
        }


        private void NavigateToUrlBoxText() {
            var navig = CurrentNavigatable;
            var urlString = urlBox.Text;
            if (navig != null && navig.CanReload && (urlString.StartsWith("http") || urlString.StartsWith("www.") || urlString.StartsWith("file://")))
                //check if current TAB can handle website
            {
                var success = navig.TryNavigate(urlString); //check if able to navigate Forward/Backward/Here in current TAB
                if (!success)
                {
                    var webbrowser = CurrentNavigatable as BrowserTab;
                    webbrowser.Navigate(urlString); //navigate to new page in current TAB
                    webbrowser.HintNewNavigation(urlString);
                    //we hint the BrowserTab's this way because it have trouble differentiating between Advertisement's URL and urlBox's URL
                }
            } else Path = urlString;
        }

        //add path to BACK/FORWARD history (skipping all checks) and update current TAB's pathString. Is called by BrowserTab.cs to indicate page have finish loading
        public void AddToHistoryStack(string finalURL, string firstURL, object obj) {
            var nav = GetINavigatableFromControl(obj);
            lastTabPaths[nav] = finalURL; //if user navigate away from this TAB, display this page when he return

            if (CurrentNavigatable == nav) //is in current TAB
            {
                if (CurrentPage != null && CurrentPage.ToString() != finalURL) backStack.Push(CurrentPage); //add current-page to HISTORY if new
                if (finalURL != firstURL && backStack.Count > 0 && backStack.Peek().ToString() == firstURL) backStack.Pop(); //remove previous-page (from HISTORY) if current-page is just a duplicate of previous-page
                CurrentPage = new NavigationStep { Path = finalURL.Split('/') }; //add new-page as current-page
            }
        }


        private void urlBox_MouseDown(object sender, MouseEventArgs e) {
            //reference: http://stackoverflow.com/questions/5014825/triple-mouse-click-in-c
            //10,000 ticks is a milisecond, therefore 2,000,000 ticks is 200milisecond . http://msdn.microsoft.com/en-us/library/system.datetime.ticks.aspx
            //double click time: http://msdn.microsoft.com/en-us/library/system.windows.forms.systeminformation.doubleclicktime(v=vs.110).aspx
            if (DateTime.Now.Ticks - lastClick <= systemDoubleClickTime) clickCount = clickCount + 1;
            else clickCount = 1;
            if (clickCount > 1) urlBox.SelectAll(); //select all text when double+ click
            lastClick = DateTime.Now.Ticks;
        }

        private class NavigationStep
        {
            public string[] Path { get; set; }

            public override string ToString() {
                return string.Join("/", Path);
            }
        }
    }
}