using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
    public partial class NavigationControl: UserControl, INotifyPropertyChanged
    {
        static List<ButtonInfo> ButtonList { get; set; }
        bool CanGoBack { get { return backStack.Any(); } }
        bool CanGoForward { get { return forwardStack.Any(); } }
        INavigatable CurrentINavigatable { get { return GetINavigatableFromControl(tabControl.SelectedTab); } }


        NavigationStep CurrentPage {
            get { return _currentPage; }
            set {
                _currentPage = value;
                PropertyChanged(this, new PropertyChangedEventArgs("CurrentPage"));
                PropertyChanged(this, new PropertyChangedEventArgs("Path"));
                foreach (var b in ButtonList) {
                    b.IsSelected = Path.StartsWith(b.TargetPath);
                    if (b.IsSelected) b.IsAlerting = false;
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
        readonly List<string> lastPaths = new List<string>();
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

                var step = GoToPage(value.Split('/'));
                if (step != null) {
                    lastPaths.Add(value);
                    if (CurrentPage != null && CurrentPage.ToString() != value) backStack.Push(CurrentPage);
                    CurrentPage = step;
                    //forwardStack.Clear();
                }

                if (value.StartsWith("http://") || value.StartsWith("https://")) Program.BrowserInterop.OpenUrl(value);

                //PropertyChanged(this, new PropertyChangedEventArgs("Path"));
            }
        }

        public NavigationControl() {
            InitializeComponent();

            ButtonList = new List<ButtonInfo>()
            {
                new ButtonInfo() { Label = "HOME", TargetPath = "http://zero-k.info/", LinkBehavior = true },
                new ButtonInfo()
                {
                    Label = "SINGLEPLAYER",
                    TargetPath = "http://zero-k.info/Missions",
                    // CONVERT Icon = HeaderButton.ButtonIcon.Singleplayer,
                    LinkBehavior = true
                },
                new ButtonInfo()
                {
                    Label = "MULTIPLAYER",
                    TargetPath = "battles", //Icon = HeaderButton.ButtonIcon.Multiplayer 
                },
                new ButtonInfo() { Label = "CHAT", TargetPath = "chat" },
                new ButtonInfo() { Label = "PLANETWARS", TargetPath = "http://zero-k.info/PlanetWars", LinkBehavior = true },
                new ButtonInfo() { Label = "MAPS", TargetPath = "http://zero-k.info/Maps", LinkBehavior = true },
                new ButtonInfo() { Label = "REPLAYS", TargetPath = "http://zero-k.info/Battles", LinkBehavior = true },
                new ButtonInfo() { Label = "SETTINGS", TargetPath = "settings" },
            };

            Instance = this;

            urlBox.DataBindings.Add("Text", this, "Path");
            tabControl.TabPages.Clear();

            chatTab = new ChatTab();
            AddTabPage(chatTab, "Chat");
            AddTabPage(new BattleListTab(), "Battles");
            AddTabPage(new SettingsTab(), "Settings");

            foreach (var but in ButtonList) {
                flowLayoutPanel1.Controls.Add(but.GetButton());
            }

            flowLayoutPanel1.BringToFront();
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

        void AddTabPage(Control content, string name = null) {
            name = name ?? content.Text ?? content.Name;
            var tb = new TabPage(name);
            tb.Dock = DockStyle.Fill;
            tb.Controls.Add(content);
            content.Dock = DockStyle.Fill;

            tabControl.TabPages.Add(tb);
        }

        INavigatable GetINavigatableFromControl(object obj) {
            if (obj is TabPage) obj = ((TabPage)obj).Controls.OfType<Control>().FirstOrDefault();
            return obj as INavigatable;
        }

        string GetLastPathStartingWith(string startString) {
            for (var i = lastPaths.Count - 1; i >= 0; i--) if (lastPaths[i].StartsWith(startString)) return lastPaths[i];
            return startString;
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
            foreach (TabPage tabPage in tabControl.Controls) {
                var navigatable = GetINavigatableFromControl(tabPage);
                if (navigatable != null && navigatable.TryNavigate(path)) {
                    tabControl.SelectTab(tabPage);
                    return new NavigationStep { Path = path };
                }
            }
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

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
    }
}