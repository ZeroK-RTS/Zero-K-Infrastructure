using System;
using System.Collections.Generic;
using System.Drawing;
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
        private const int TopRightMiniIconSize = 32;
        private const int TopRightMiniIconMargin = 6;
        private const int TopRightSpace = 200;
        private readonly Stack<NavigationStep> backStack = new Stack<NavigationStep>();
        private readonly BitmapButton btnBack;
        private readonly BitmapButton btnForward;
        private readonly Stack<NavigationStep> forwardStack = new Stack<NavigationStep>();
        private readonly Timer isBusyTimer = new Timer();
        private readonly Dictionary<INavigatable, string> lastTabPaths = new Dictionary<INavigatable, string>();
        private readonly int systemDoubleClickTime = SystemInformation.DoubleClickTime*10000;

        private readonly HeadlessTabControl tabControl;
        private readonly ZklTextBox urlBox;

        private NavigationStep _currentPage;

        private int clickCount;
        private long lastClick;

        public NavigationControl() {
            SuspendLayout();
            urlBox = new ZklTextBox();
            BorderStyle = BorderStyle.None;
            var flowLayoutPanel1 = new FlowLayoutPanel();
            btnForward = new BitmapButton();
            btnBack = new BitmapButton();
            tabControl = new HeadlessTabControl();
            // 
            // urlBox
            // 
            urlBox.Location = new Point(166, 34);
            urlBox.Size = new Size(190, 20);
            urlBox.TabIndex = 2;
            urlBox.Font = Config.GeneralFontSmall;
            urlBox.KeyDown += urlBox_KeyDown;
            urlBox.MouseDown += urlBox_MouseDown;

            var table = new TableLayoutPanel();
            table.RowCount = 1;
            table.ColumnCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TopRightSpace));
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Controls.Add(table);


            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoScroll = false;
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanel1.BackColor = Color.Transparent;
            flowLayoutPanel1.Dock = DockStyle.Top;
            flowLayoutPanel1.MinimumSize = new Size(300, 28);
            flowLayoutPanel1.Padding = new Padding(13);
            flowLayoutPanel1.WrapContents = false;
            // 
            // btnForward
            // 

            btnForward.BackColor = Color.Transparent;
            //this.btnForward.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnForward.BackgroundImage")));
            btnForward.BackgroundImageLayout = ImageLayout.Stretch;
            btnForward.ButtonStyle = FrameBorderRenderer.StyleType.DarkHive;
            btnForward.Cursor = Cursors.Hand;
            btnForward.FlatStyle = FlatStyle.Flat;
            btnForward.ForeColor = Color.White;
            btnForward.Location = new Point(85, 34);
            btnForward.Name = "btnForward";
            btnForward.Size = new Size(75, 23);
            btnForward.SoundType = SoundPalette.SoundType.Click;
            btnForward.TabIndex = 4;
            btnForward.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnForward.UseVisualStyleBackColor = true;
            btnForward.Click += btnForward_Click;
            btnForward.ButtonStyle = FrameBorderRenderer.StyleType.IconOnly;
            btnForward.Image = ZklResources.smurf;

            // 
            // btnBack
            // 
            btnBack.BackColor = Color.Transparent;
            //this.btnBack.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnBack.BackgroundImage")));
            btnBack.BackgroundImageLayout = ImageLayout.Stretch;
            btnBack.ButtonStyle = FrameBorderRenderer.StyleType.DarkHive;
            btnBack.Cursor = Cursors.Hand;
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.ForeColor = Color.White;
            btnBack.Location = new Point(4, 34);
            btnBack.Name = "btnBack";
            btnBack.Size = new Size(75, 23);
            btnBack.SoundType = SoundPalette.SoundType.Click;
            btnBack.TabIndex = 3;
            btnBack.Text = "Back";
            btnBack.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnBack.UseVisualStyleBackColor = true;
            btnBack.Click += btnBack_Click;
            // 
            // tabControl
            // 
            tabControl.Dock = DockStyle.Fill;
            tabControl.Selecting += tabControl_Selecting;
            // 
            // NavigationControl
            // 
            table.Controls.Add(flowLayoutPanel1, 0, 0);

            Controls.Add(btnForward);
            Controls.Add(btnBack);
            Controls.Add(urlBox);
            Controls.Add(tabControl);
            Margin = new Padding(0);
            Name = "NavigationControl";
            Size = new Size(703, 219);
            ResumeLayout(false);
            PerformLayout();

            isBusyTimer.Interval = 120; //timer tick to update "isBusyIcon" every 120 ms.
            isBusyTimer.Tick += (sender, args) => { Application.UseWaitCursor = CurrentNavigatable.IsBusy; };
            isBusyTimer.Start();

            Instance = this;

            SetupTabButtons(flowLayoutPanel1);
            InitializeTabPageContent();

            

            var minMaxButton = new BitmapButton
            {
                ButtonStyle = FrameBorderRenderer.StyleType.IconOnly,
                SoundType = SoundPalette.SoundType.Click,
                Height = TopRightMiniIconSize,
                Width = TopRightMiniIconSize,
                Top = 0,
                Margin = new Padding(TopRightMiniIconMargin),
                Image = Buttons.win_min.GetResizedWithCache(32, 32)
            };
            minMaxButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            minMaxButton.Left = Width - 70 - 5;

            minMaxButton.Click += (sender, args) =>
            {
                var mw = Program.MainWindow;
                if (mw != null)
                {
                    if (mw.WindowState == FormWindowState.Maximized)
                    {
                        mw.WindowState = FormWindowState.Normal;
                        mw.FormBorderStyle = FormBorderStyle.Sizable;
                        mw.TopMost = false;
                        minMaxButton.Image = Buttons.win_max.GetResizedWithCache(60, 60);
                    } else
                    {
                        mw.WindowState = FormWindowState.Maximized;
                        mw.FormBorderStyle = FormBorderStyle.None;
                        mw.TopMost = true;
                        minMaxButton.Image = Buttons.win_min.GetResizedWithCache(60, 60);
                    }
                }
            };

            table.Controls.Add(minMaxButton, 1, 0);
            //Controls.Add(minMaxButton);            

            //flowLayoutPanel1.Controls.Add(minMaxButton);
            //flowLayoutPanel1.BringToFront();

            ResumeLayout();
        }

        private static void SetupTabButtons(Control control) {
            ButtonList = new List<ButtonInfo> //normal arrangement
            {
                new ButtonInfo { Label = "WEB", TargetPath = GlobalConst.BaseSiteUrl + "/", Icon = Buttons.extras, Height = TabButtonHeight, Width = 200 },
                new ButtonInfo
                {
                    Label = "SINGLEPLAYER",
                    TargetPath = string.Format("{0}/Missions", GlobalConst.BaseSiteUrl),
                    Icon = Buttons.sp,
                    Width = 250,
                    Height = TabButtonHeight
                },
                new ButtonInfo { Label = "MULTIPLAYER", TargetPath = "battles", Icon = Buttons.mp, Width = 250, Height = TabButtonHeight },
                new ButtonInfo { Label = "CHAT", TargetPath = "chat", Icon = Buttons.chat, Height = TabButtonHeight, Width = 200 }
                /*new ButtonInfo
                {
                    Label = "SETTINGS",
                    TargetPath = "settings",
                    Icon = Buttons.settings,
                    Height = 70,
                    Width = 250,
                    Dock = DockStyle.Right
                }*/
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
                    AddTabPage(new BrowserTab(GlobalConst.BaseSiteUrl + "/Missions", false), "sp");
                    AddTabPage(new BrowserTab(GlobalConst.BaseSiteUrl + "/Battles", false), "rp");
                    AddTabPage(new BrowserTab(GlobalConst.BaseSiteUrl + "/Planetwars", false), "pw");
                    AddTabPage(new BrowserTab(GlobalConst.BaseSiteUrl + "/Forum", true), "fm");
                }
                var home = AddTabPage(new BrowserTab(GlobalConst.BaseSiteUrl, true), "hm");
                tabControl.SelectTab(home);
            }
            var battles = new BattleListTab();
            AddTabPage(battles, "Battles");
            AddTabPage(new SettingsTab(), "Settings");
            AddTabPage(new ServerTab(), "Server");
            AddTabPage(new DownloaderTab(), "Rapid");
            AddTabPage(new ExtrasTab(), "Extra");
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
            BackColor = Color.Black;
            //this.RenderParentsBackgroundImage(e);
            base.OnPaintBackground(e);
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, Bounds, FrameBorderRenderer.StyleType.TechPanel);
            //var nb = new Rectangle(tabControl.Left, tabControl.Top+23, tabControl.Width, tabControl.Height-23);
            //new LinearGradientBrush(new Rectangle(0, 0, 1, 1), Color.FromArgb(255, 19, 65, 73), Color.FromArgb(255, 0, 0, 0), 90)

            //nb.Inflate(7,6);
            //FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, nb, FrameBorderRenderer.StyleType.Shraka);
            var b = Bounds;
            //b.Intersect(new Rectangle(Bounds.X + 10, Bounds.Y + 10, Bounds.Width - 20, Bounds.Height - 20));
            //FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, b, FrameBorderRenderer.StyleType.TechPanel);
        }


        private void btnBack_Click(object sender, EventArgs e) {
            NavigateBack();
        }

        private void btnForward_Click(object sender, EventArgs e) {
            NavigateForward();
        }

        private void tabControl_Selecting(object sender, TabControlCancelEventArgs e) {
            //is called from NavigationControl.Designer.cs when Tab is selected
            //Path = e.TabPage.Text; //this return TAB's name (eg: chat, pw, battle). NOTE: not needed because BUTTON press will call SwitchTab() which also started the navigation
            //e.Cancel = true;
        }

        private void urlBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyData == Keys.Return)
            {
                NavigateToUrlBoxText();
                e.Handled = true;
            }
        }

        private void reloadButton1_Click(object sender, EventArgs e) //make webpage refresh
        {
            var navig = CurrentNavigatable;
            if (navig != null && navig.CanReload) navig.Reload();
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

        private void logoutButton_Click(object sender, EventArgs e) {
            Program.TasClient.RequestDisconnect();
            Program.Conf.LobbyPlayerPassword = "";
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