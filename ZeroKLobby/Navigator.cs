using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby.Controls;
using ZeroKLobby.MapDownloader;
using ZeroKLobby.MicroLobby;
using ZkData;

namespace ZeroKLobby
{
    public class Navigator
    {
        readonly Stack<string> backStack = new Stack<string>();
        readonly ChatTab chatTab;
        readonly Stack<string> forwardStack = new Stack<string>();
        readonly Dictionary<INavigatable, string> lastTabPaths = new Dictionary<INavigatable, string>();
        public ChatTab ChatTab
        {
            get { return chatTab; }
        }


        static List<ButtonInfo> ButtonList { get; set; }
        bool CanGoBack
        {
            get { return backStack.Any(); }
        }
        bool CanGoForward
        {
            get { return forwardStack.Any(); }
        }


        string path = String.Empty;

        public string Path
        {
            get
            {
                return path;
            }
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

                if (value == path) return;
                
                if (value.StartsWith("www."))
                {
                    value = "http://" + value;
                } //create "http://www"

                if (value.StartsWith("http://") || value.StartsWith("https://") || value.StartsWith("file://")) {
                    Program.BrowserInterop.OpenUrl(value); //this open external browser
                } else {
                    path = value;
                    if (string.IsNullOrEmpty(value)) {
                        ButtonList.ForEach(x => x.IsSelected = false); //unselect all button
                    }
                    {
                        foreach (TabPage tabPage in tabs.Controls) {
                            var navigatable = GetINavigatableFromControl(tabPage); //translate tab button into the page it represent
                            if (navigatable != null && navigatable.TryNavigate(path.Split('/'))) {
                                UpdateTabButtons(path);
                                tabs.SelectTab(tabPage);
                                lastTabPaths[navigatable] = path;
                                SetHeader(navigatable.Title);
                                backStack.Push(path);
                            }
                        }
                    }
                }

            }
        }

        void UpdateTabButtons(string newPath)
        {
            ButtonList.ForEach(x => x.IsSelected = false); //unselect all button

            var selbut = ButtonList.Where(x => newPath.StartsWith(x.TargetPath)).OrderByDescending(x => x.TargetPath.Length).FirstOrDefault();
            if (selbut != null) {
                selbut.IsSelected = true;
                selbut.IsAlerting = false;
            }

            var navigable = tabs.Controls.OfType<Object>().Select(GetINavigatableFromControl).FirstOrDefault(x => x != null && newPath.StartsWith(x.PathHead));
            //find TAB with correct PathHead
            if (navigable != null) navigable.Hilite(HiliteLevel.None, newPath); //cancel hilite ChatTab's tab (if meet some condition)
        }

        Control buttonPanel;
        TabControl tabs;

        public Navigator(TabControl tabs, Control buttonPanel)
        {
            this.buttonPanel = buttonPanel;
            this.tabs = tabs;

            //(Increase performance), Reference: http://msdn.microsoft.com/en-us/library/system.windows.forms.control.suspendlayout.aspx

            ButtonList = new List<ButtonInfo>() //normal arrangement
            {
                new ButtonInfo() { Label = "Chat", TargetPath = "chat", Icon = ZklResources.chat},
                new ButtonInfo() { Label = "Quick browse", TargetPath = "battles", Icon = ZklResources.battle },
                new ButtonInfo() { Label = "Extras", TargetPath = "extras", Icon= Buttons.map.GetResizedWithCache(18,18) },
                new ButtonInfo() {Label = "Settings",TargetPath = "settings",Icon = Buttons.settings.GetResizedWithCache(18,18)},
            };

            foreach (var b in ButtonList) buttonPanel.Controls.Add(b.GetButton());

            tabs.TabPages.Clear();

            chatTab = new ChatTab();

            lastTabPaths[chatTab] = string.Format("chat/channel/{0}",
                Program.Conf != null ? Program.Conf.AutoJoinChannels.OfType<string>().FirstOrDefault() : "zk");
            AddTabPage(chatTab, "Chat");

            var battles = new BattleListTab();
            AddTabPage(battles, "Battles");
            AddTabPage(new SettingsTab(), "Settings");
            AddTabPage(new ServerTab(), "Server");
            AddTabPage(new DownloaderTab(), "Rapid");
        }



        public bool HilitePath(string navigationPath, HiliteLevel hiliteLevel)
        {
            if (string.IsNullOrEmpty(navigationPath)) return false;
            if (hiliteLevel == HiliteLevel.Flash) foreach (var b in ButtonList) if (navigationPath.StartsWith(b.TargetPath)) b.IsAlerting = true; //make BUTTON turn red

            var navigable = tabs.Controls.OfType<Object>().Select(GetINavigatableFromControl).First(x => x != null && navigationPath.Contains(x.PathHead));
            if (navigable != null) return navigable.Hilite(hiliteLevel, navigationPath); //make ChatTab's tab to flash
            else return false;
        }

        public void NavigateBack()
        {
            if (CanGoBack) GoBack();
        }

        public void NavigateForward()
        {
            if (CanGoForward) GoForward();
        }

        public INavigatable GetNavigatableFromPath(string path)
        {
            return tabs.Controls.OfType<Control>().Select(GetINavigatableFromControl).FirstOrDefault(x => path.StartsWith(x.PathHead));
        }

        public void SwitchTab(string targetPath)
        { //called by ButtonInfo.cs when clicked. "targetPath" is usually a "PathHead"
            foreach (TabPage tabPage in tabs.Controls)
            {
                var nav = GetINavigatableFromControl(tabPage);
                if (nav.PathHead == targetPath)
                {
                    if (CurrentNavigatable == nav)
                    {
                        Path = targetPath; // double click on forum go to forum home
                    }
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


        TabPage AddTabPage(Control content, string name = null)
        {
            name = name ?? content.Text ?? content.Name;
            var tb = new TransparentTabPage();
            tb.Dock = DockStyle.Fill;
            tb.Controls.Add(content);
            content.Dock = DockStyle.Fill;
            tabs.TabPages.Add(tb);
            return tb;
        }

        INavigatable GetINavigatableFromControl(object obj)
        {
            if (obj is TabPage) obj = ((TabPage)obj).Controls.OfType<Control>().FirstOrDefault();
            return obj as INavigatable;
        }


        void GoBack()
        {
            if (forwardStack.Count == 0 || forwardStack.Peek() != Path) forwardStack.Push(Path);
            Path = backStack.Pop();
        }

        void GoForward()
        {
            if (backStack.Count == 0 || backStack.Peek() != Path) backStack.Push(Path);
            Path = forwardStack.Pop();
        }

        void SetHeader(string text)
        {
            Program.MainWindow.lbRightPanelTitle.Text = text;
        }


        public INavigatable CurrentNavigatable
        {
            get { return tabs.SelectedTab.Controls.OfType<INavigatable>().FirstOrDefault(); }
        }
    }
}