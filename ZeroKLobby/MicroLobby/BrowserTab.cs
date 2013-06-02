using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby
{
    public class BrowserTab : WebBrowser, INavigatable
    {
        bool initLoad;
        int navigatedIndex = 0;
        readonly List<string> navigatedPlaces = new List<string>();
        string navigatingTo = null;
        readonly string pathHead;

        public BrowserTab(string head)
        {
            pathHead = head;
            if (Program.TasClient != null) Program.TasClient.LoginAccepted += (sender, args) =>
            {
                initLoad = true;
                navigatingTo = head;
                Navigate(head);
            };
        }


        protected override void OnNavigated(WebBrowserNavigatedEventArgs e)
        {
            base.OnNavigated(e);
            if (navigatingTo == e.Url.ToString())
            {
                if (!initLoad) {
                    var final = navigatingTo;
                    if (navigatedIndex == navigatedPlaces.Count) navigatedPlaces.Add(final);
                    else navigatedPlaces[navigatedIndex] = final;
                    navigatedIndex++;
                    Program.MainWindow.navigationControl.Path = Uri.UnescapeDataString(final);
                }
                initLoad = false;
            }
        }

        protected override void OnNavigating(WebBrowserNavigatingEventArgs e)
        {
            var url = e.Url.ToString();
            if (string.IsNullOrEmpty(e.TargetFrameName) && url.Contains("zero-k") && !url.StartsWith("javascript:"))
            {
                var nav = Program.MainWindow.navigationControl.GetInavigatableByPath(url);
                if (nav == null || nav == this) {
                    navigatingTo = url;
                }
                else
                {
                    // navigate to another tab actually
                    e.Cancel = true;
                    Program.MainWindow.navigationControl.Path = url;
                }
            }
            base.OnNavigating(e);
        }

        public string PathHead { get { return pathHead; } }

        public bool TryNavigate(bool reload,params string[] path)
        {
            var pathString = String.Join("/", path);
            if (!pathString.StartsWith(PathHead)) return false;
            if (reload) return NavRefresh();
            var url = Url != null ? Url.ToString() : "";
            if (navigatingTo == pathString || pathString == url) return true; // already navigating there

            navigatingTo = pathString;
            Navigate(pathString);
            return true;
        }

        public bool NavRefresh()
        {
            Refresh(WebBrowserRefreshOption.IfExpired);
            return true;
        }

        public bool Hilite(HiliteLevel level, params string[] path)
        {
            return false;
        }

        public string GetTooltip(params string[] path)
        {
            throw new NotImplementedException();
        }
    }
}