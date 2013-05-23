using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby
{
    public class BrowserTab: WebBrowser, INavigatable
    {
        bool initLoad;
        int navigatedIndex = 0;
        readonly List<string> navigatedPlaces = new List<string>();
        string navigatingTo = null;
        string orgNavTo;
        readonly string pathHead;

        public BrowserTab(string head) {
            pathHead = head;
            Program.TasClient.LoginAccepted += (sender, args) =>
                {
                    initLoad = true;
                    Navigate(head); // todo handle change of login name -> first user scenarios! - preload on logi change        
                };
        }


        protected override void OnNavigated(WebBrowserNavigatedEventArgs e) {
            base.OnNavigated(e);
            if (navigatingTo == Program.BrowserInterop.AddAuthToUrl(e.Url.ToString())) {
                if (!initLoad) {
                    if (navigatedIndex == navigatedPlaces.Count) navigatedPlaces.Add(orgNavTo);
                    else navigatedPlaces[navigatedIndex] = orgNavTo;
                    navigatedIndex++;
                    Program.MainWindow.navigationControl.Path = Uri.UnescapeDataString(orgNavTo);
                }
                initLoad = false;
            }
        }

        protected override void OnNavigating(WebBrowserNavigatingEventArgs e) {
            var url = e.Url.ToString();
            if (string.IsNullOrEmpty(e.TargetFrameName) && url.ToLower().Contains("zero-k.info")) {
                var nav = Program.MainWindow.navigationControl.GetInavigatableByPath(url);
                if (nav == null || nav == this) {
                    orgNavTo = url;
                    navigatingTo = Program.BrowserInterop.AddAuthToUrl(orgNavTo);
                }
                else {
                    // navigate to another tab actually
                    e.Cancel = true;
                    Program.MainWindow.navigationControl.Path = url;
                }
            }
            base.OnNavigating(e);
        }

        public string PathHead { get { return pathHead; } }

        public bool TryNavigate(params string[] path) {
            var pathString = String.Join("/", path);
            if (!pathString.StartsWith(PathHead)) return false;
            var url = Url != null ? Url.ToString() : "";
            if (navigatingTo == pathString || Program.BrowserInterop.AddAuthToUrl(pathString) == navigatingTo ||
                Program.BrowserInterop.AddAuthToUrl(pathString) == Program.BrowserInterop.AddAuthToUrl(url)) return true; // already navigating there
            Navigate(pathString);
            return true;
        }

        public bool Hilite(HiliteLevel level, params string[] path) {
            return false;
        }

        public string GetTooltip(params string[] path) {
            throw new NotImplementedException();
        }
    }
}