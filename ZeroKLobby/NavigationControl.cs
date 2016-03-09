using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby.Controls;
using ZeroKLobby.MapDownloader;
using ZeroKLobby.MicroLobby;
using ZkData;

namespace ZeroKLobby
{
    public partial class NavigationControl: ZklBaseControl
    {
        private Timer isBusyTimer = new Timer();
        static List<ButtonInfo> ButtonList { get; set; }
        bool CanGoBack { get { return backStack.Any(); } }
        bool CanGoForward { get { return forwardStack.Any(); } }


        NavigationStep CurrentPage {
            get { return _currentPage; }
            set {
                _currentPage = value;
                urlBox.Text = Path;

                ButtonList.ForEach(x=>x.IsSelected = false); //unselect all button

                var selbut = ButtonList.Where(x => Path.StartsWith(x.TargetPath)).OrderByDescending(x => x.TargetPath.Length).FirstOrDefault();
                if (selbut != null) {
                    selbut.IsSelected = true;
                    selbut.IsAlerting = false;
                }


                var navigable = tabControl.Controls.OfType<Object>().Select(GetINavigatableFromControl).FirstOrDefault(x => x != null && Path.StartsWith(x.PathHead)); //find TAB with correct PathHead
                if (navigable != null) navigable.Hilite(HiliteLevel.None, Path); //cancel hilite ChatTab's tab (if meet some condition)
            }
        }

        NavigationStep _currentPage;
        readonly Stack<NavigationStep> backStack = new Stack<NavigationStep>();
        readonly ChatTab chatTab;
        readonly Stack<NavigationStep> forwardStack = new Stack<NavigationStep>();
        readonly Dictionary<INavigatable, string> lastTabPaths = new Dictionary<INavigatable, string>();
        public ChatTab ChatTab { get { return chatTab; } }
        public static NavigationControl Instance { get; private set; }

        public string Path {
            get { return CurrentPage != null ? CurrentPage.ToString() : string.Empty; }
            set {
                if (value.ToLower().StartsWith("zk://")) value = value.Substring(5);

                var parts = value.Split('@');
                for (var i = 1; i < parts.Length; i++) {
                    var action = parts[i];
                    ActionHandler.PerformAction(action);
                }
                value = parts[0];

                if (CurrentPage != null && CurrentPage.ToString() == value) return; // we are already there, no navigation needed

                if (value.StartsWith("www.")) { value = "http://" + value; } //create "http://www"
                var step = GoToPage(value.Split('/')); //go to page
                if (step != null) {
                    if (CurrentPage != null && CurrentPage.ToString() != value) backStack.Push(CurrentPage);
                    CurrentPage = step;
                }
                else if (value.StartsWith("http://") || value.StartsWith("https://") ||  value.StartsWith("file://"))
                {
                    Program.BrowserInterop.OpenUrl(value); //this open external browser
                } 
            }
        }

        private ZeroKLobby.HeadlessTabControl tabControl;
        private System.Windows.Forms.TextBox urlBox;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private BitmapButton btnBack;
        private BitmapButton btnForward;
        private BitmapButton reloadButton1;

        public NavigationControl() {
            SuspendLayout();
            this.urlBox = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.reloadButton1 = new ZeroKLobby.BitmapButton();
            this.btnForward = new ZeroKLobby.BitmapButton();
            this.btnBack = new ZeroKLobby.BitmapButton();
            this.tabControl = new ZeroKLobby.HeadlessTabControl();
            this.SuspendLayout();
            // 
            // urlBox
            // 
            this.urlBox.Location = new System.Drawing.Point(166, 34);
            this.urlBox.Name = "urlBox";
            this.urlBox.Size = new System.Drawing.Size(190, 20);
            this.urlBox.TabIndex = 2;
            this.urlBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.urlBox_KeyDown);
            this.urlBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.urlBox_MouseDown);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.MinimumSize = new System.Drawing.Size(300, 28);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(13);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(703, 28);
            this.flowLayoutPanel1.TabIndex = 5;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // reloadButton1
            // 
            this.reloadButton1.BackColor = System.Drawing.Color.Transparent;
            //this.reloadButton1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("reloadButton1.BackgroundImage")));
            this.reloadButton1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.reloadButton1.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.reloadButton1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.reloadButton1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.reloadButton1.ForeColor = System.Drawing.Color.White;
            this.reloadButton1.Location = new System.Drawing.Point(403, 34);
            this.reloadButton1.Name = "reloadButton1";
            this.reloadButton1.Size = new System.Drawing.Size(58, 23);
            this.reloadButton1.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.reloadButton1.TabIndex = 7;
            this.reloadButton1.Text = "Refresh";
            this.reloadButton1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.reloadButton1.UseVisualStyleBackColor = true;
            this.reloadButton1.Visible = false;
            this.reloadButton1.Click += new System.EventHandler(this.reloadButton1_Click);
            // 
            // btnForward
            // 
            this.btnForward.BackColor = System.Drawing.Color.Transparent;
            //this.btnForward.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnForward.BackgroundImage")));
            this.btnForward.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnForward.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnForward.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnForward.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnForward.ForeColor = System.Drawing.Color.White;
            this.btnForward.Location = new System.Drawing.Point(85, 34);
            this.btnForward.Name = "btnForward";
            this.btnForward.Size = new System.Drawing.Size(75, 23);
            this.btnForward.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnForward.TabIndex = 4;
            this.btnForward.Text = "Forward";
            this.btnForward.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnForward.UseVisualStyleBackColor = true;
            this.btnForward.Click += new System.EventHandler(this.btnForward_Click);
            // 
            // btnBack
            // 
            this.btnBack.BackColor = System.Drawing.Color.Transparent;
            //this.btnBack.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnBack.BackgroundImage")));
            this.btnBack.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnBack.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnBack.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.ForeColor = System.Drawing.Color.White;
            this.btnBack.Location = new System.Drawing.Point(4, 34);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(75, 23);
            this.btnBack.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnBack.TabIndex = 3;
            this.btnBack.Text = "Back";
            this.btnBack.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Location = new System.Drawing.Point(0, 42);
            this.tabControl.Margin = new System.Windows.Forms.Padding(0);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Drawing.Point(0, 0);
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(703, 185);
            this.tabControl.TabIndex = 0;
            this.tabControl.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl_Selecting);
            // 
            // NavigationControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.DimGray;
            this.Controls.Add(this.reloadButton1);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.btnForward);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.urlBox);
            this.Controls.Add(this.tabControl);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "NavigationControl";
            this.Size = new System.Drawing.Size(703, 219);
            this.Resize += new System.EventHandler(this.NavigationControl_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();



            isBusyTimer.Interval = 120; //timer tick to update "isBusyIcon" every 120 ms.
            isBusyTimer.Tick += (sender, args) => { Cursor = CurrentNavigatable.IsBusy ? Cursors.AppStarting : DefaultCursor; };
            isBusyTimer.Start();

            ButtonList = new List<ButtonInfo>() //normal arrangement
            {
                new ButtonInfo() { Label = "WEB", TargetPath = GlobalConst.BaseSiteUrl + "/", Icon= Buttons.extras, Height = 70,Width = 200 },
                new ButtonInfo()
                {
                    Label = "SINGLEPLAYER",
                    TargetPath = string.Format("{0}/Missions", GlobalConst.BaseSiteUrl),
                    Icon = Buttons.sp,
                    Width = 250,
                    Height = 70,
                },
                new ButtonInfo()
                {
                    Label = "MULTIPLAYER",
                    TargetPath = "battles", Icon =  Buttons.mp,
                    Width = 250,
                    Height = 70,
                },
                new ButtonInfo() { Label = "CHAT", TargetPath = "chat", Icon= Buttons.chat, Height = 70, Width = 200 },

                new ButtonInfo() { Label = "SETTINGS", TargetPath = "settings", Icon = Buttons.settings, Height = 70, Width = 250, Dock = DockStyle.Right},
               
            };

            Instance = this;

            tabControl.TabPages.Clear();

            chatTab = new ChatTab();
            
            lastTabPaths[chatTab] = string.Format("chat/channel/{0}", Program.Conf != null ? Program.Conf.AutoJoinChannels.OfType<string>().FirstOrDefault():"zk");
            AddTabPage(chatTab, "Chat");
            if (Environment.OSVersion.Platform != PlatformID.Unix && !Program.Conf.UseExternalBrowser) {
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
                reloadButton1.Visible = true;
            }
            var battles = new BattleListTab();
            AddTabPage(battles, "Battles");
            AddTabPage(new SettingsTab(), "Settings");
            AddTabPage(new ServerTab(), "Server");
            AddTabPage(new DownloaderTab(), "Rapid");
            AddTabPage(new MicroLobby.ExtrasTab.ExtrasTab(), "Extra");
            
            foreach (var but in ButtonList) flowLayoutPanel1.Controls.Add(but.GetButton());
            var minMaxButton = new BitmapButton()
            {
                ButtonStyle = FrameBorderRenderer.StyleType.DarkHive, SoundType = SoundPalette.SoundType.Click,
                Height = 70, Width = 70, Image = Buttons.win_min.GetResizedWithCache(60,60)
            };
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

            flowLayoutPanel1.Controls.Add(minMaxButton);
            flowLayoutPanel1.BringToFront();
            ResumeLayout();
        }

        public INavigatable GetInavigatableByPath(string path) { //get which TAB has which PathHead (header)
            foreach (TabPage tabPage in tabControl.Controls) {
                var navigatable = GetINavigatableFromControl(tabPage);
                if (path.Contains(navigatable.PathHead)) return navigatable;
            }
            return null;
        }


        public bool HilitePath(string navigationPath, HiliteLevel hiliteLevel) {
            if (string.IsNullOrEmpty(navigationPath)) return false;
            if (hiliteLevel == HiliteLevel.Flash) foreach (var b in ButtonList) if (navigationPath.StartsWith(b.TargetPath)) b.IsAlerting = true; //make BUTTON turn red

            var navigable = tabControl.Controls.OfType<Object>().Select(GetINavigatableFromControl).First(x => x != null && navigationPath.Contains(x.PathHead));
            if (navigable != null) return navigable.Hilite(hiliteLevel, navigationPath); //make ChatTab's tab to flash
            else return false;
        }

        public void NavigateBack() {
            if (CanGoBack) GoBack();
        }

        public void NavigateForward() {
            if (CanGoForward) GoForward();
        }

        public void SwitchTab(string targetPath) { //called by ButtonInfo.cs when clicked. "targetPath" is usually a "PathHead"
            foreach (TabPage tabPage in tabControl.Controls) {
                var nav = GetINavigatableFromControl(tabPage);
                if (nav.PathHead == targetPath)
                {
                    if (CurrentNavigatable == nav) {
                        Path = targetPath; // double click on forum go to forum home
                    }
                    else {
                        string lastPath;
                        if (lastTabPaths.TryGetValue(nav, out lastPath)) targetPath = lastPath; //go to current page of the tab
                        Path = targetPath;
                    }
                    return;
                }
            }
            Path = targetPath;
        }


        TabPage AddTabPage(Control content, string name = null) {
            name = name ?? content.Text ?? content.Name;
            var tb = new TabPage(name);
            tb.Dock = DockStyle.Fill;
            tb.Controls.Add(content);
            content.Dock = DockStyle.Fill;
            tabControl.TabPages.Add(tb);
            return tb;
        }

        INavigatable GetINavigatableFromControl(object obj) {
            if (obj is TabPage) obj = ((TabPage)obj).Controls.OfType<Control>().FirstOrDefault();
            return obj as INavigatable;
        }


        void GoBack() {
            if (forwardStack.Count == 0 || forwardStack.Peek().ToString() != CurrentPage.ToString()) forwardStack.Push(CurrentPage);
            CurrentPage = backStack.Pop();
            GoToPage(CurrentPage.Path);
        }

        void GoForward() {
            if (backStack.Count == 0 || backStack.Peek().ToString() != CurrentPage.ToString()) backStack.Push(CurrentPage);
            CurrentPage = forwardStack.Pop();
            GoToPage(CurrentPage.Path);
        }


        NavigationStep GoToPage(string[] path) // todo cleanup
        {
            foreach (TabPage tabPage in tabControl.Controls)
            {
                var navigatable = GetINavigatableFromControl(tabPage); //translate tab button into the page it represent
                if (navigatable != null && navigatable.TryNavigate(path))
                {
                    tabControl.SelectTab(tabPage);
                    reloadButton1.Visible = navigatable.CanReload;
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


        void btnBack_Click(object sender, EventArgs e) {
            NavigateBack();
        }

        void btnForward_Click(object sender, EventArgs e) {
            NavigateForward();
        }

        void tabControl_Selecting(object sender, TabControlCancelEventArgs e) { //is called from NavigationControl.Designer.cs when Tab is selected
            //Path = e.TabPage.Text; //this return TAB's name (eg: chat, pw, battle). NOTE: not needed because BUTTON press will call SwitchTab() which also started the navigation
            //e.Cancel = true;
        }

        void urlBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyData == Keys.Return) {
                goButton1_Click(sender, e);
                e.Handled = true;
            }
        }

        class NavigationStep
        {
            public string[] Path { get; set; }

            public override string ToString() {
                return string.Join("/", Path);
            }
        }

        private void NavigationControl_Resize(object sender, EventArgs e)
        {
            // todo  instead add flowlayoutpanel or tablelayout panel to entire navigation form and let i size elements as needed

            int windowWidth = this.Size.Width;
            int windowHeight = this.Size.Height;

            //this make back/forward/reload button follow Nav bar's height (this is not really important if Nav bar height remain constant)
            //NOTE: tweak here if not satisfy with Go/Forward/Backward button position. This override designer.
            flowLayoutPanel1.Width = windowWidth;
            int height = flowLayoutPanel1.Size.Height;
            btnBack.Location = new System.Drawing.Point(btnBack.Location.X, height);
            btnForward.Location = new System.Drawing.Point(btnForward.Location.X, height);
            urlBox.Location = new System.Drawing.Point(urlBox.Location.X, height);
            reloadButton1.Location = new System.Drawing.Point(reloadButton1.Location.X, height);

            //resize the content area (which show chat & internal browser) according to Nav bar's height
            int heightPlusButton = height + btnBack.Height - tabControl.ItemSize.Height;
            int freeHeight = windowHeight - heightPlusButton;
            tabControl.Location = new System.Drawing.Point(10, heightPlusButton + 10);
            tabControl.Height = freeHeight - 20;
            tabControl.Width = windowWidth - 20;
        }

        public INavigatable CurrentNavigatable { get { return tabControl.SelectedTab.Controls.OfType<INavigatable>().FirstOrDefault(); } }

        private void reloadButton1_Click(object sender, EventArgs e) //make webpage refresh
        {
            var navig = CurrentNavigatable;
            if (navig != null && navig.CanReload) navig.Reload();
        }

        private void goButton1_Click(object sender, EventArgs e)
        {
            var navig = CurrentNavigatable;
            string urlString = urlBox.Text;
            if (navig != null && navig.CanReload && (urlString.StartsWith("http") || urlString.StartsWith("www.") || urlString.StartsWith("file://"))) //check if current TAB can handle website
            {
                bool success = navig.TryNavigate(urlString); //check if able to navigate Forward/Backward/Here in current TAB
                if (!success)
                {
                    BrowserTab webbrowser = CurrentNavigatable as BrowserTab;
                    webbrowser.Navigate(urlString); //navigate to new page in current TAB
                    webbrowser.HintNewNavigation(urlString); //we hint the BrowserTab's this way because it have trouble differentiating between Advertisement's URL and urlBox's URL
                }
            }
            else {  Path = urlString; } //perform general & common navigation specific to TAB (go to TAB and perform action)
        }

        //add path to BACK/FORWARD history (skipping all checks) and update current TAB's pathString. Is called by BrowserTab.cs to indicate page have finish loading
        public void AddToHistoryStack(String finalURL, String firstURL, Object obj)
        {
            INavigatable nav = GetINavigatableFromControl(obj);
            lastTabPaths[nav] = finalURL;//if user navigate away from this TAB, display this page when he return

            if (CurrentNavigatable == nav) //is in current TAB
            {
                if (CurrentPage != null && CurrentPage.ToString() != finalURL) backStack.Push(CurrentPage); //add current-page to HISTORY if new
                if (finalURL != firstURL && backStack.Count > 0 && backStack.Peek().ToString() == firstURL) backStack.Pop(); //remove previous-page (from HISTORY) if current-page is just a duplicate of previous-page
                CurrentPage = new NavigationStep { Path = finalURL.Split('/') }; //add new-page as current-page
            }
        }

        private void logoutButton_Click(object sender, EventArgs e)
        {
            Program.TasClient.RequestDisconnect();
            Program.Conf.LobbyPlayerPassword = "";
        }



        private int clickCount = 0;
        private long lastClick = 0;
        private int systemDoubleClickTime = SystemInformation.DoubleClickTime * 10000;
        private void urlBox_MouseDown(object sender, MouseEventArgs e)
        {
            //reference: http://stackoverflow.com/questions/5014825/triple-mouse-click-in-c
            //10,000 ticks is a milisecond, therefore 2,000,000 ticks is 200milisecond . http://msdn.microsoft.com/en-us/library/system.datetime.ticks.aspx
            //double click time: http://msdn.microsoft.com/en-us/library/system.windows.forms.systeminformation.doubleclicktime(v=vs.110).aspx
            if (DateTime.Now.Ticks - lastClick <= systemDoubleClickTime) clickCount = clickCount + 1;
            else clickCount = 1;
            if (clickCount % 3 == 0) urlBox.SelectAll(); //select all text when triple click
            lastClick = DateTime.Now.Ticks;
        }
    }
}