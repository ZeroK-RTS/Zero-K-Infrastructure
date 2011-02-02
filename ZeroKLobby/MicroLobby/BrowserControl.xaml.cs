using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Navigation;
using ZeroKLobby.Notifications;
using ZkData;
using Control = System.Windows.Controls.Control;
using Cursors = System.Windows.Input.Cursors;
using UserControl = System.Windows.Controls.UserControl;
using WebBrowser = System.Windows.Controls.WebBrowser;

namespace ZeroKLobby.MicroLobby
{
	/// <summary>
	/// Interaction logic for MissionControl.xaml
	/// </summary>
	public partial class BrowserControl: UserControl, INavigatable
	{
		
		public BrowserControl()
		{
      try {
        WindowsApi.InternetSetCookie(Config.BaseUrl, GlobalConst.LobbyAccessCookieName, "1");
      } catch (Exception ex) {
        Trace.TraceWarning("Unable to set ZK cookie:{0}", ex);
      }
      if (Process.GetCurrentProcess().ProcessName == "devenv") return;
      InitializeComponent();
		}


		public string PathHead { get { return "http://"; } }

		string navigatingTo = null;

	  List<string> navigatedPlaces = new List<string>();
	  int navigatedIndex = 0;
    bool navigating = false;

		public bool TryNavigate(params string[] path)
		{

      var pathString = String.Join("/", path);
			if (!pathString.StartsWith(PathHead)) return false;
			if (WebBrowser.Source != null && pathString == WebBrowser.Source.OriginalString) return true;

      if (navigatingTo == pathString) return true; // already navigating there

      if (navigatedIndex > 1 && navigatedPlaces[navigatedIndex-2] == pathString)
      {
        navigatedIndex-=2;
        WebBrowser.GoBack();
        return true;
      }
      
      //navigatingTo = pathString;
			WebBrowser.Navigate(pathString);
	
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

		void WebBrowser_Navigated(object sender, NavigationEventArgs e)
		{
      navigating = false;
      navigatingTo = null;
      if (navigatedIndex == navigatedPlaces.Count) navigatedPlaces.Add(e.Uri.ToString());
      else navigatedPlaces[navigatedIndex] = e.Uri.ToString();
		  navigatedIndex++;
		}

		void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
		{
      navigating = true;
	    if (navigatingTo != e.Uri.ToString())
			{
				navigatingTo = e.Uri.ToString();
				if (navigatingTo.Contains("@"))
				{
				  e.Cancel = true;
          navigatingTo = null;
          navigating = false;
				}
				Program.MainWindow.navigationControl.Path = Uri.UnescapeDataString(e.Uri.ToString());
			}
		}

		private void webBrowser_Loaded(object sender, RoutedEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
			UrlSecurityZone.InternetSetFeatureEnabled(UrlSecurityZone.InternetFeaturelist.DISABLE_NAVIGATION_SOUNDS, UrlSecurityZone.SetFeatureOn.PROCESS, true);
			//UrlSecurityZone.InternetSetFeatureEnabled(UrlSecurityZone.InternetFeaturelist.OBJECT_CACHING, UrlSecurityZone.SetFeatureOn.PROCESS, false);
			
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
		}
	}
}