using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby.MapDownloader;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
    public partial class NavigationControl: UserControl
    {
        static List<ButtonInfo> ButtonList { get; set; }
        bool CanGoBack { get { return backStack.Any(); } }
        bool CanGoForward { get { return forwardStack.Any(); } }


        NavigationStep CurrentPage {
            get { return _currentPage; }
            set {
                _currentPage = value;
                urlBox.Text = Path;

                ButtonList.ForEach(x=>x.IsSelected = false);
                
                var selbut = ButtonList.Where(x => Path.StartsWith(x.TargetPath)).OrderByDescending(x => x.TargetPath.Length).FirstOrDefault();
                if (selbut != null) {
                    selbut.IsSelected = true;
                    selbut.IsAlerting = false;
                }


                var steps = Path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries); // todo cleanup
                var navigable =
                    tabControl.Controls.OfType<Object>().Select(GetINavigatableFromControl).FirstOrDefault(x => x != null && x.PathHead == steps[0]);
                if (navigable != null) navigable.Hilite(HiliteLevel.None, steps);
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

                var step = GoToPage(value.Split('/')); //go to page and is not reload
                if (step != null) {
                    if (CurrentPage != null && CurrentPage.ToString() != value) backStack.Push(CurrentPage);
                    CurrentPage = step;
                }
                else if (value.StartsWith("http://") || value.StartsWith("https://")) Program.BrowserInterop.OpenUrl(value);
            }
        }

        public NavigationControl() {
            InitializeComponent();

            ButtonList = new List<ButtonInfo>()
            {
                new ButtonInfo() { Label = "HOME", TargetPath = "http://zero-k.info/", Icon= Buttons.home, Height = 32,},
                new ButtonInfo()
                {
                    Label = "SINGLEPLAYER",
                    TargetPath = "http://zero-k.info/Missions",
                    Icon = Buttons.spherebot,
                    Width = 128,
                    Height = 32,
                },
                new ButtonInfo()
                {
                    Label = "MULTIPLAYER",
                    TargetPath = "battles", Icon =  ZklResources.battle,
                    Width = 128,
                    Height = 32,
                },
                new ButtonInfo() { Label = "CHAT", TargetPath = "chat", Icon= ZklResources.chat, Height = 32, },
                new ButtonInfo() { Label = "PLANETWARS", TargetPath = "http://zero-k.info/PlanetWars", Height = 32,  },
                new ButtonInfo() { Label = "MAPS", TargetPath = "http://zero-k.info/Maps", Icon = Buttons.map, Height = 32,  },
                new ButtonInfo() { Label = "REPLAYS", TargetPath = "http://zero-k.info/Battles", Icon = Buttons.video_icon, Height = 32, },
                new ButtonInfo() { Label = "FORUM", TargetPath = "http://zero-k.info/Forum", Height = 32, },
                new ButtonInfo() { Label = "SETTINGS", TargetPath = "settings", Icon = Buttons.settings, Height = 32, },
            };

            Instance = this;

            tabControl.TabPages.Clear();

            chatTab = new ChatTab();
            
            lastTabPaths[chatTab] = string.Format("chat/channel/{0}", Program.Conf != null ? Program.Conf.AutoJoinChannels.OfType<string>().FirstOrDefault():"zk");
            AddTabPage(chatTab, "Chat");
            if (Environment.OSVersion.Platform != PlatformID.Unix && !Program.Conf.UseExternalBrowser) {
                AddTabPage(new BrowserTab("http://zero-k.info/Maps"), "Maps");
                AddTabPage(new BrowserTab("http://zero-k.info/Missions"), "sp");
                AddTabPage(new BrowserTab("http://zero-k.info/Battles"), "rp");
                AddTabPage(new BrowserTab("http://zero-k.info/PlanetWars"), "pw");
                AddTabPage(new BrowserTab("http://zero-k.info/Forum"), "fm");
                var home = AddTabPage(new BrowserTab("http://zero-k.info/"), "hm");
                tabControl.SelectTab(home);
            }
            var battles = new BattleListTab();
            AddTabPage(battles, "Battles");
            AddTabPage(new SettingsTab(), "Settings");
            AddTabPage(new ServerTab(), "Server");
            AddTabPage(new AdvertiserWindow(), "Advertiser");
            AddTabPage(new DownloaderTab(), "Rapid");
            
            foreach (var but in ButtonList) flowLayoutPanel1.Controls.Add(but.GetButton());
            foreach (Control button in flowLayoutPanel1.Controls) {
                button.Margin = new Padding(0,0,0,3);
                button.Cursor = Cursors.Hand;
            }
            flowLayoutPanel1.BringToFront();
        }

        public INavigatable GetInavigatableByPath(string path) {
            foreach (TabPage tabPage in tabControl.Controls) {
                var navigatable = GetINavigatableFromControl(tabPage);
                if (path.Contains(navigatable.PathHead)) return navigatable;
            }
            return null;
        }


        public bool HilitePath(string navigationPath, HiliteLevel hiliteLevel) {
            if (string.IsNullOrEmpty(navigationPath)) return false;
            if (hiliteLevel == HiliteLevel.Flash) foreach (var b in ButtonList) if (navigationPath.StartsWith(b.TargetPath)) b.IsAlerting = true;

            var steps = navigationPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var navigable =
                tabControl.Controls.OfType<Object>().Select(GetINavigatableFromControl).FirstOrDefault(x => x != null && x.PathHead == steps[0]);
            if (navigable != null) return navigable.Hilite(hiliteLevel, steps);
            else return false;
        }

        public void NavigateBack() {
            if (CanGoBack) GoBack();
        }

        public void NavigateForward() {
            if (CanGoForward) GoForward();
        }

        public void SwitchTab(string targetPath) {
            foreach (TabPage tabPage in tabControl.Controls) {
                var nav = GetINavigatableFromControl(tabPage);
                if (nav.PathHead == targetPath) {
                    if (CurrentNavigatable == nav) {
                        Path = targetPath; // double click on forum go to forum home
                    }
                    else {
                        string lastPath;
                        if (lastTabPaths.TryGetValue(nav, out lastPath)) targetPath = lastPath;
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
                var navigatable = GetINavigatableFromControl(tabPage);
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
            if (CanGoForward) GoForward();
        }

        void tabControl_Selecting(object sender, TabControlCancelEventArgs e) {
            Path = e.TabPage.Text.ToLower();
            //e.Cancel = true;
        }

        void urlBox_Enter(object sender, EventArgs e) {
            urlBox.SelectAll();
        }

        void urlBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyData == Keys.Return) {
                Path = urlBox.Text;
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
            // todo  instead add flowlayoytpanel or tablelayout panel to entire navigation form and let i size elements as needed

            //make back/forward/reload button follow Nav bar auto resize (in other word: dynamic repositioning)
            int height = flowLayoutPanel1.Size.Height;
            btnBack.Location = new System.Drawing.Point(btnBack.Location.X, height);
            btnForward.Location = new System.Drawing.Point(btnForward.Location.X, height);
            urlBox.Location = new System.Drawing.Point(urlBox.Location.X, height);
            reloadButton1.Location = new System.Drawing.Point(reloadButton1.Location.X, height);

            //resize the "browser" (that show chat & internal browser) according to Nav bar auto resize (dynamic resizing)
            int windowHeight = this.Size.Height;
            int freeHeight = windowHeight - height;
            tabControl.Location = new System.Drawing.Point(tabControl.Location.X, height);
            tabControl.Height = freeHeight;
        }

        public INavigatable CurrentNavigatable { get { return tabControl.SelectedTab.Controls.OfType<INavigatable>().FirstOrDefault(); } }

        private void reloadButton1_Click(object sender, EventArgs e) //make webpage refresh
        {
            var navig = CurrentNavigatable;
            if (navig != null && navig.CanReload) navig.Reload();
        }

    }
}