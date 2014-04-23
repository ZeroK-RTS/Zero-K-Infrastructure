using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby
{
    public class BrowserTab : ExtendedWebBrowser, INavigatable //INavigatable as (WebBrowser as) ExtendedWebBrowser as BrowserTab //added ExtendedWebBrowser <--- WebBrowser
    {
        int navigatedIndex = 0;
        int historyCount = 0;
        int currrentHistoryPosition = 0;
        List<string> navigatedPlaces = new List<string>();
        List<string> historyList = new List<string>();
        string navigatingTo = null; //URL that we want to go (assigned in: TryNavigate, NavigationControl.goButton1_Click, & OnNavigating). Will NOT be same as final URL if website redirect us
        bool finishNavigation = true;
        readonly string pathHead;

        public BrowserTab(string head, bool autoStartOnLogin)
        {
            pathHead = head;
            if (Program.TasClient != null && autoStartOnLogin==true) Program.TasClient.LoginAccepted += (sender, args) =>
            {
                HintNewNavigation(head);
                base.Navigate(head);
            };
            base.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(browser_DocumentCompleted); //This will call "UpdateURL()" when page finish loading
            base.NewWindow3 += BrowserTab_NewWindow3;
            this.ScriptErrorsSuppressed = true;
        }

        void BrowserTab_NewWindow3(object sender, NewWindow3EventArgs e)
        {
            if (Program.Conf.InterceptPopup) //any new window to be redirected internally?
            {
                NavigationControl.Instance.Path = e.Url.ToString();
                e.Cancel = true;
            }
        }

        //protected override void OnNewWindow(System.ComponentModel.CancelEventArgs e) //This block "Open In New Window" button.
        //{
        //    e.Cancel = true;
        //    Navigate(StatusText);
        //}

        //protected override void OnNavigated(WebBrowserNavigatedEventArgs e) //This intercept which URL finish loading (including Advertisement)
        //{
        //    base.OnNavigated(e);
        //}

        protected override void OnNavigating(WebBrowserNavigatingEventArgs e) //this intercept URL navigation induced when user click on link or during page loading  (including Advertisement)
        {
            if (finishNavigation) //if HintNewNavigation() was never called (meaning user's navigation was by clicking URL, and not thru UrlBox and not thru NavigationControl.cs button): Will do the following processing:
            {
                var url = e.Url.ToString();
                if (string.IsNullOrEmpty(e.TargetFrameName) && url.StartsWith("http://zero-k.info") && !url.StartsWith("javascript:")) //if navigation is within Zero-K
                {
                    var nav = Program.MainWindow.navigationControl.GetInavigatableByPath(url); //check which TAB this URL represent
                    if (url.Contains("@logout"))
                    {
                        //if Logout signature, perform logout
                        e.Cancel = true;
                        ActionHandler.PerformAction("logout");
                        Program.MainWindow.navigationControl.Path = url.Replace("@logout","");
                    }
                    else if (nav == null || nav == this || url.Contains("/SubmitPost?"))
                    {
                        //if url belong to this TAB or not other TAB, or is posting comment in this TAB, continue this browser instance uninterupted
                        //HintNewNavigation(url);
                    }
                    else
                    {
                        // else, navigate to another tab actually
                        e.Cancel = true;
                        Program.MainWindow.navigationControl.Path = url;
                    }
                }
                else if (url.StartsWith("javascript:SendLobbyCommand('"))
                {
                    // intercept & not trigger the javascript, instead execute it directly from the url 
                    //(because for unknown reason mission/replay can't be triggered more than once using standard technique(javascript send text to lobby to trigger mission))
                    e.Cancel = true;

                    int endPosition = url.IndexOf("');void(0);", 29); //the end of string as read from Internet Browser status bar
                    int commandLength = endPosition - 29; //NOTE: "javascript:SendLobbyCommand('" is 30 char. So the startPos in at 29th char
                    Program.MainWindow.navigationControl.Path = url.Substring(29, commandLength);
                }

                if (!e.Cancel)
                {
                    HintNewNavigation(url);
                }
            }

            base.OnNavigating(e);
        }


        public string PathHead { get { return pathHead; } }

        public bool TryNavigate(params string[] path) //navigation induced by call from "NavigationControl.cs"
        {
            String pathString = String.Join("/", path);

            if (navigatingTo == pathString) { return true; }  //already navigating there, just return TRUE
            if (this.Url != null && !string.IsNullOrEmpty(this.Url.ToString()))
            {
                String currentURL = this.Url.ToString();
                if (pathString == currentURL) { return true; } //already there, just return TRUE
            }
            if (pathString.StartsWith(PathHead)) 
            {
                TryNavigateORBackForward(pathString);
                return true; //the URL is intended header or is children of intended header, return TRUE and Navigate() to URL
            }
            bool beenThere = HaveVisitedBefore(pathString);
            if (beenThere)
            {
                TryNavigateORBackForward(pathString);
                return true; //the URL is from history, return TRUE and Navigate() to URL
            }
            return false; //URL has nothing to do with this WebBrowser instance,  just return FALSE
        }

        public bool Hilite(HiliteLevel level, string path)
        {
            return false;
        }

        public string GetTooltip(params string[] path)
        {
            throw new NotImplementedException();
        }

        public void Reload() {
            base.Refresh(WebBrowserRefreshOption.IfExpired);
        }

        public bool CanReload { get { return true; }}

        private void browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e) //is called when webpage finish loading
        {   //Reference: http://msdn.microsoft.com/en-us/library/system.windows.forms.webbrowser.aspx

            if (!finishNavigation)
            {
                string originalURL = navigatingTo;
                string finalURL = this.Url.ToString();

                //This update URL textbox & add the page to NavigationBar's history (so that Back&Forward button can be used):
                Program.MainWindow.navigationControl.AddToHistoryStack(finalURL, originalURL, this);

                //The following code store previously visited URL for checking in TryNavigate() later. The checking determine which TAB "own" the URL.
                //This list is unordered (it is not related to sequence in "Forward"/"Backward" button)
                bool beenThere = HaveVisitedBefore(finalURL);
                if (!beenThere)
                {
                    navigatedPlaces.Add(finalURL);//if at end of table then use "Add"
                    navigatedIndex++;
                }
                AddToHistory(finalURL);
            }

            finishNavigation = true;
            ResumeLayout();
        }

        //HISTORY CONTROL & CHECK SECTION//
        public void HintNewNavigation(String newPath)
        {
            navigatingTo = newPath;
            finishNavigation = false;
        }
        
        //this function tell whether this WebBrowser have been in the specified URL
        private bool HaveVisitedBefore(String pathString)
        {
            for (int i = 0; i < navigatedIndex; i++)
            {
                if (navigatedPlaces[i] == pathString)
                {
                    return true; //the URL is from history, return TRUE
                }
            }
            return false; //the URL is new, return FALSE
        }

        //this function keep track of new pages opened by WebBrowser and translate it into history stack.
        //(We do this because there's difficulty with accessing WebBrowser's history directly)
        private void AddToHistory(String pathString) 
        {
            if (currrentHistoryPosition <= historyCount)
            {
                if (currrentHistoryPosition == 0 && historyCount == 0)
                {
                    historyList.Add(pathString);
                    historyCount++;
                }
                else
                {
                    if (currrentHistoryPosition > 0 && historyList[currrentHistoryPosition - 1] == pathString)
                    {
                        //IS GOING BACK
                        currrentHistoryPosition = currrentHistoryPosition - 1;
                    }
                    else if (currrentHistoryPosition < historyCount - 1 && historyList[currrentHistoryPosition + 1] == pathString)
                    {
                        //IS GOING FORWARD
                        currrentHistoryPosition = currrentHistoryPosition + 1;
                    }
                    else if (historyList[currrentHistoryPosition] == pathString)
                    {
                        //IS NOT GOING ANYWHERE
                    }
                    else
                    {
                        //IS NEW PAGE
                        if (currrentHistoryPosition == historyCount - 1)
                        {
                            //IS NEW PAGE AT EDGE OF HISTORY
                            historyList.Add(pathString);
                            currrentHistoryPosition = historyCount;
                            historyCount++;
                        }
                        else if (currrentHistoryPosition < historyCount - 1)
                        {
                            //IS NEW PAGE SOMEWHERE AT MID OF HISTORY
                            historyList[currrentHistoryPosition + 1] = pathString;
                            currrentHistoryPosition = currrentHistoryPosition + 1;
                        }
                    }
                }
            }
        }

        //this function compare the pathString with one in history, and determine whether WebBrowser should GoBack() or GoForward()
        //NavigationControl.cs (which controls the "Forward" and "Back" button) call TryNavigate() which in turn call TryToGoBackForward()
        private void TryNavigateORBackForward(String pathString)
        {
            SuspendLayout(); //pause layout until page loaded for probably some performance improvement?.
            HintNewNavigation(pathString);

            bool usesBackForwardOption = false; 
            if (currrentHistoryPosition <= historyCount)
            {
                if (currrentHistoryPosition != 0 || historyCount != 0)
                {
                    if (currrentHistoryPosition > 0 && historyList[currrentHistoryPosition - 1] == pathString)
                    {
                        //GO BACK
                        usesBackForwardOption = true;
                        currrentHistoryPosition = currrentHistoryPosition - 1;
                        base.GoBack();
                    }
                    else if (currrentHistoryPosition < historyCount - 1 && historyList[currrentHistoryPosition + 1] == pathString)
                    {
                        //GO FORWARD
                        usesBackForwardOption = true;
                        currrentHistoryPosition = currrentHistoryPosition + 1;
                        base.GoForward();
                    }
                    else
                    {
                        //System.Diagnostics.Trace.TraceInformation("currrentHistoryPosition {0}", currrentHistoryPosition);
                        //System.Diagnostics.Trace.TraceInformation("historyCount {0}", historyCount);
                        //System.Diagnostics.Trace.TraceInformation("Newpage {0}", pathString);
                    }
                }
            }
            if (!usesBackForwardOption)
            {
                //NEW NAVIGATE
                base.Navigate(pathString);
            }
            return;
        }
        
    }
}