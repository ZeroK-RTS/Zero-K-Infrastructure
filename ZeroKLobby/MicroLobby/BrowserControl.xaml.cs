using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Navigation;
using ZeroKLobby.Notifications;
using ZkData;
using Control = System.Windows.Controls.Control;
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
			InitializeComponent();
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
		}


		public string PathHead { get { return "http://"; } }

		string navigatingTo = null;

		public bool TryNavigate(params string[] path)
		{
			var pathString = String.Join("/", path);
			if (!pathString.StartsWith(PathHead)) return false;
			if (WebBrowser.Source != null && pathString == WebBrowser.Source.OriginalString && pathString == "http://zero-k.info/Missions.mvc") return true;
			if (navigatingTo != pathString)
			{
				navigatingTo = pathString;
				WebBrowser.Navigate(pathString);
			}
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
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
		}

		void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
			if (navigatingTo != e.Uri.ToString())
			{
				navigatingTo = e.Uri.ToString();
				if (navigatingTo.Contains("@")) e.Cancel = true;
				Program.MainWindow.navigationControl.Path = e.Uri.ToString();
			}
		}

		private void webBrowser_Loaded(object sender, RoutedEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
			WindowsApi.InternetSetCookie(Config.BaseUrl, GlobalConst.LobbyAccessCookieName, "1");
			UrlSecurityZone.InternetSetFeatureEnabled(UrlSecurityZone.InternetFeaturelist.DISABLE_NAVIGATION_SOUNDS, UrlSecurityZone.SetFeatureOn.PROCESS, true);
			//UrlSecurityZone.InternetSetFeatureEnabled(UrlSecurityZone.InternetFeaturelist.OBJECT_CACHING, UrlSecurityZone.SetFeatureOn.PROCESS, false);
			
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
		}
	}
}