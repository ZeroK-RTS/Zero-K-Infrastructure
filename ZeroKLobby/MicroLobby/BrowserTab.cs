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
            base.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(UpdateURL); //This will call "UpdateURL()" when page finish loading
        }

        //protected override void OnNewWindow(System.ComponentModel.CancelEventArgs e) //This block "Open In New Window" button.
        //{
        //    e.Cancel = true;
        //    Navigate(StatusText);
        //}


        //protected override void OnNavigated(WebBrowserNavigatedEventArgs e) //This intercept which URL finish loading (including Advertisement)
        //{
        //    base.OnNavigated(e);
        //    if (navigatingTo == e.Url.ToString()) //this store visited URL in a list:
        //    {
        //        if (!initLoad)
        //        {
        //            var final = navigatingTo;
        //            if (navigatedIndex == navigatedPlaces.Count) navigatedPlaces.Add(final);
        //            else navigatedPlaces[navigatedIndex] = final;
        //            navigatedIndex++;
        //            Program.MainWindow.navigationControl.Path = Uri.UnescapeDataString(final);
        //        }
        //        initLoad = false;
        //    }
        //}
        
        protected override void OnNavigating(WebBrowserNavigatingEventArgs e) //this intercept URL navigation induced when user click on link or during page loading
        {
            var url = e.Url.ToString();
            if (string.IsNullOrEmpty(e.TargetFrameName) && url.Contains("zero-k") && !url.StartsWith("javascript:")) //if navigation is within Zero-K
            {
                var nav = Program.MainWindow.navigationControl.GetInavigatableByPath(url); //check which TAB this URL represent
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

        public bool TryNavigate(params string[] path) //navigation induced by call from "NavigationControl.cs"
        {
            var pathString = String.Join("/", path);
            if (navigatingTo == pathString) { return true; }  //already navigating there, just return TRUE
            if (pathString.StartsWith(PathHead)) 
            {
                navigatingTo = pathString;
                Navigate(pathString);
                return true; //the URL contain intended header, return TRUE and Navigate() to URL
            }
            if (Url != null && !string.IsNullOrEmpty(Url.ToString()))
            {
                var url = Url.ToString();
                if (pathString == url) { return true; } //already there, just return TRUE
            }
            int count = 0;
            foreach (String previousURL in navigatedPlaces)
            {
                if (previousURL == pathString) 
                {
                    navigatingTo = pathString;
                    Navigate(pathString);
                    return true; //the URL is from history, return TRUE and Navigate() to URL
                }
                count++;
            }
            return false; //URL has nothing to do with this WebBrowser instance,  just return FALSE
        }

        public bool Hilite(HiliteLevel level, params string[] path)
        {
            return false;
        }

        public string GetTooltip(params string[] path)
        {
            throw new NotImplementedException();
        }

        public void Reload() {
            Refresh(WebBrowserRefreshOption.IfExpired);
        }

        public bool CanReload { get { return true; }}

        private void UpdateURL(object sender, WebBrowserDocumentCompletedEventArgs e) //is called when webpage finish loading
        {   //Reference: http://msdn.microsoft.com/en-us/library/system.windows.forms.webbrowser.aspx
            
            //This update URL textbox & add the page to NavigationBar's history (so that Back&Forward button can be used):
            navigatingTo = ((WebBrowser)sender).Url.ToString();
            Program.MainWindow.navigationControl.Path = ((WebBrowser)sender).Url.ToString();
            //NOTE on operation: 
            //setting the "path" to URL will cause "GoToPage(path)" to be called, 
            //"GoToPage(path)" then will loop over all tab control (including this instance of "BrowserTab"), which will then call "TryToNavigate(path)".
            //SINCE the "navigatingTo" is already set to this URL here, so when "TryToNavigate(path)" is called it matches to the current URL and return TRUE.
            //when "TryToNavigate(path)" return TRUE it will cause the page to be pushed to stack & the Back button will work.
            
            if (!initLoad) //skip the first URL (?)
            {      
                //This store previously visited URL for checking later. This list is unordered (it will have duplicate entry & not related at all to sequence in Forward/Backward button)
                var final = navigatingTo;
                if (navigatedIndex == navigatedPlaces.Count) { navigatedPlaces.Add(final); }//if at end of table then use "Add"
                else navigatedPlaces[navigatedIndex] = final;
                navigatedIndex++;
            }
            initLoad = false;
        }
         
    }
}