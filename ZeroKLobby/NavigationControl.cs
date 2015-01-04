using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby.MapDownloader;
using ZeroKLobby.MicroLobby;
using ZkData;

namespace ZeroKLobby
{
    public partial class NavigationControl: UserControl
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
                if (value.ToLower().StartsWith("spring://")) value = value.Substring(9);

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

        public NavigationControl() {
            SuspendLayout();//(Increase performance), Reference: http://msdn.microsoft.com/en-us/library/system.windows.forms.control.suspendlayout.aspx
            InitializeComponent();

            isBusyTimer.Interval = 1020; //timer tick to update "isBusyIcon" every 1020 ms.
            isBusyTimer.Tick += isBusyTimer_Tick;
            isBusyTimer.Start();

            ButtonList = new List<ButtonInfo>() //normal arrangement
            {
                new ButtonInfo() { Label = "HOME", TargetPath = GlobalConst.BaseSiteUrl, Icon= Buttons.home, Height = 32,Width = 80 },
                new ButtonInfo() { Label = "CHAT", TargetPath = "chat", Icon= ZklResources.chat, Height = 32, Width = 65 },
                new ButtonInfo()
                {
                    Label = "SINGLEPLAYER",
                    TargetPath = string.Format("{0}Missions", GlobalConst.BaseSiteUrl),
                    Icon = Buttons.spherebot,
                    Width = 125,
                    Height = 32,
                },
                new ButtonInfo()
                {
                    Label = "MULTIPLAYER",
                    TargetPath = "battles", Icon =  ZklResources.battle,
                    Width = 115,
                    Height = 32,
                },
                
                new ButtonInfo() { Label = "PLANETWARS", TargetPath = "http://zero-k.info/Planetwars", Height = 32,  },
                new ButtonInfo() { Label = "MAPS", TargetPath = "http://zero-k.info/Maps", Icon = Buttons.map, Height = 32, Width = 75 },
                new ButtonInfo() { Label = "REPLAYS", TargetPath = "http://zero-k.info/Battles", Icon = Buttons.video_icon, Height = 32, Width = 95 },
                new ButtonInfo() { Label = "FORUM", TargetPath = "http://zero-k.info/Forum", Height = 32, Width = 65, },
                new ButtonInfo() { Label = "SETTINGS", TargetPath = "settings", Icon = Buttons.settings, Height = 32, Width = 100, Dock = DockStyle.Right},
               
            };

            Instance = this;

            tabControl.TabPages.Clear();

            chatTab = new ChatTab();
            
            lastTabPaths[chatTab] = string.Format("chat/channel/{0}", Program.Conf != null ? Program.Conf.AutoJoinChannels.OfType<string>().FirstOrDefault():"zk");
            AddTabPage(chatTab, "Chat");
            if (Environment.OSVersion.Platform != PlatformID.Unix && !Program.Conf.UseExternalBrowser) {
                if (!Program.Conf.SingleInstance) //run in multiple TAB?
                {
                    AddTabPage(new BrowserTab("http://zero-k.info/Maps", false), "Maps");
                    AddTabPage(new BrowserTab("http://zero-k.info/Missions", false), "sp");
                    AddTabPage(new BrowserTab("http://zero-k.info/Battles", false), "rp");
                    AddTabPage(new BrowserTab("http://zero-k.info/Planetwars", false), "pw");
                    AddTabPage(new BrowserTab("http://zero-k.info/Forum", true), "fm");
                }
                var home = AddTabPage(new BrowserTab("http://zero-k.info/", true), "hm");
                tabControl.SelectTab(home);
                if (Program.Conf.InterceptPopup) 
                {
                    AddTabPage(new BrowserTab("http", false), "other"); //a tab with generic match that match 100% of random URL (block new window)
                    ButtonList.Add(new ButtonInfo() { Label = "OTHER", TargetPath = "http", Height = 32, Width = 65,});
                }
                reloadButton1.Visible = true;
            }
            var battles = new BattleListTab();
            AddTabPage(battles, "Battles");
            AddTabPage(new SettingsTab(), "Settings");
            AddTabPage(new ServerTab(), "Server");
            AddTabPage(new DownloaderTab(), "Rapid");
            AddTabPage(new MicroLobby.ExtrasTab.ExtrasTab(), "Extra");
            
            foreach (var but in ButtonList) flowLayoutPanel1.Controls.Add(but.GetButton());
            flowLayoutPanel1.Controls.Add(logoutButton);
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
            goButton1.Location = new System.Drawing.Point(goButton1.Location.X, height);
            isBusyIcon.Location = new System.Drawing.Point(isBusyIcon.Location.X, height);

            //resize the content area (which show chat & internal browser) according to Nav bar's height
            int heightPlusButton = height + btnBack.Height - tabControl.ItemSize.Height;
            int freeHeight = windowHeight - heightPlusButton;
            tabControl.Location = new System.Drawing.Point(tabControl.Location.X, heightPlusButton);
            tabControl.Height = freeHeight;
            tabControl.Width = windowWidth;
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
            Program.TasClient.Disconnect();
            Program.Conf.LobbyPlayerName = "";
        }


        private void isBusyTimer_Tick(object sender, EventArgs e)
        {
            isBusyIcon.Visible = CurrentNavigatable.IsBusy;
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