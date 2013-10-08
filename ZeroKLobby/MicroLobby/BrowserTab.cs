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
        readonly List<string> navigatedPlaces = new List<string>();
        readonly List<string> historyList = new List<string>();
        string navigatingTo = null;
        readonly string pathHead;

        public BrowserTab(string head)
        {
            pathHead = head;
            if (Program.TasClient != null) Program.TasClient.LoginAccepted += (sender, args) =>
            {
                navigatingTo = head;
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
            if (url.StartsWith("javascript:SendLobbyCommand('"))
            {
                // intercept & not trigger the javascript, instead execute it directly from the url 
                //(because for unknown reason mission/replay can't be triggered more than once using standard technique(javascript send text to lobby to trigger mission))
                e.Cancel = true;

                int endPosition = url.IndexOf("');void(0);", 29);
                int commandLength = endPosition - 29; //NOTE: "javascript:SendLobbyCommand('" is 30 char. So the startPos in at 29th char
                Program.MainWindow.navigationControl.Path = url.Substring(29, commandLength);
            }
            base.OnNavigating(e);
        }

        public string PathHead { get { return pathHead; } }

        public bool TryNavigate(params string[] path) //navigation induced by call from "NavigationControl.cs"
        {
            String pathString = String.Join("/", path);
            if (navigatingTo == pathString) { return true; }  //already navigating there, just return TRUE
            if (pathString.StartsWith(PathHead)) 
            {
                SuspendLayout(); //pause layout until page loaded. //Reference: http://msdn.microsoft.com/en-us/library/system.windows.forms.control.suspendlayout.aspx
                bool canNavigate = TryToGoBackForward(pathString);
                if (canNavigate) { return true; }
                navigatingTo = pathString;
                base.Navigate(pathString);
                return true; //the URL is intended header or is children of intended header, return TRUE and Navigate() to URL
            }
            String currentURL = String.Empty;
            if (Url != null && !string.IsNullOrEmpty(Url.ToString()))
            {
                currentURL = Url.ToString();
                if (pathString == currentURL) { return true; } //already there, just return TRUE
            }
            for (int i = 0; i < navigatedIndex; i++)
            {
                if (navigatedPlaces[i] == pathString) 
                {
                    SuspendLayout();
                    bool canNavigate = TryToGoBackForward(pathString);
                    if (canNavigate) {return true;}
                    navigatingTo = pathString;
                    base.Navigate(pathString);
                    return true; //the URL is from history, return TRUE and Navigate() to URL
                }
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
            
            //This update URL textbox & add the page to NavigationBar's history (so that Back&Forward button can be used):
            navigatingTo = ((WebBrowser)sender).Url.ToString();
            Program.MainWindow.navigationControl.AddToHistoryStack(navigatingTo,this);
            ResumeLayout();
            
            //The following code store previously visited URL for checking in TryNavigate() later. The checking determine which TAB "own" the URL.
            //This list is unordered (it is not related to sequence in "Forward"/"Backward" button)
            var final = navigatingTo;
            bool inList = false;
            for (int i = 0; i < navigatedIndex; i++)
            {
                if (navigatedPlaces[i] == final)
                {
                    inList = true;
                    break;
                }
            }
            if (!inList)
            {
                navigatedPlaces.Add(final);//if at end of table then use "Add"
                navigatedIndex++;
            }

            AddToHistory(final);

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
                    bool isBack = false;
                    bool isFront = false;
                    bool isHere = false;
                    if (currrentHistoryPosition > 0 && historyList[currrentHistoryPosition - 1] == pathString)
                    {
                        isBack = true;
                        currrentHistoryPosition = currrentHistoryPosition - 1;
                    }
                    else if (currrentHistoryPosition < historyCount - 1 && historyList[currrentHistoryPosition + 1] == pathString)
                    {
                        isFront = true;
                        currrentHistoryPosition = currrentHistoryPosition + 1;
                    }
                    else if (historyList[currrentHistoryPosition] == pathString)
                    {
                        isHere = true;
                    }
                    if (!isHere && !isBack && !isFront)
                    {
                        if (currrentHistoryPosition == historyCount - 1)
                        {
                            historyList.Add(pathString);
                            currrentHistoryPosition = historyCount;
                            historyCount++;
                        }
                        else if (currrentHistoryPosition < historyCount - 1)
                        {
                            historyList[currrentHistoryPosition + 1] = pathString;
                            currrentHistoryPosition = currrentHistoryPosition + 1;
                        }
                    }
                }
            }
        }

        //this function compare the pathString with one in history, and determine whether WebBrowser should GoBack() or GoForward()
        //NavigationControl.cs (which controls the "Forward" and "Back" button) call TryNavigate() and in turn call TryToGoBackForward()
        private bool TryToGoBackForward(String pathString)
        {
            if (currrentHistoryPosition <= historyCount)
            {
                if (currrentHistoryPosition != 0 || historyCount != 0)
                {
                    bool isBack = false;
                    bool isFront = false;
                    if (currrentHistoryPosition > 0 && historyList[currrentHistoryPosition - 1] == pathString)
                    {
                        isBack = true;
                    }
                    else if (currrentHistoryPosition < historyCount - 1 && historyList[currrentHistoryPosition + 1] == pathString)
                    {
                        isFront = true;
                    }
                    if (isBack)
                    {
                        currrentHistoryPosition = currrentHistoryPosition - 1;
                        base.GoBack();
                        //System.Diagnostics.Trace.TraceInformation("GoBack {0}", pathString);
                        navigatingTo = pathString;
                        return true;
                    }
                    else if (isFront)
                    {
                        currrentHistoryPosition = currrentHistoryPosition + 1;
                        base.GoForward();
                        //System.Diagnostics.Trace.TraceInformation("GoForward {0}", pathString);
                        navigatingTo = pathString;
                        return true;
                    }
                    else
                    {
                        //System.Diagnostics.Trace.TraceInformation("currrentHistoryPosition {0}", currrentHistoryPosition);
                        //System.Diagnostics.Trace.TraceInformation("historyCount {0}", historyCount);
                        //System.Diagnostics.Trace.TraceInformation("Newpage {0}", pathString);
                    }
                }
            }
            return false;
        }
        
    }
}