using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Navigation;
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
		}


		void PerformAction(string actionString)
		{
			var parts = actionString.Split(':');
			if (parts.Length == 2)
			{
				if (parts[0] == "start_mission")
				{
					int missionID;
					if (int.TryParse(parts[1], out missionID)) StartMission(missionID);
				}
			}
		}

		void StartMission(int missionID)
		{
			// client.GetMissionByIDAsync(missionID);
			MessageBox.Show("Not implemented");
		}

		public string PathHead { get { return "http://zero-k.info/Missions.mvc"; } }

		public bool TryNavigate(params string[] path)
		{
			var pathString = String.Join("/", path);
			if (!pathString.StartsWith(PathHead)) return false;
			if (WebBrowser.Source != null && pathString != WebBrowser.Source.OriginalString) WebBrowser.Navigate(pathString);
			return true;
		}

		void WebBrowser_Navigated(object sender, NavigationEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
			NavigationControl.Instance.Path = e.Uri.OriginalString;
		}

		void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
			var parts = e.Uri.OriginalString.Split('@');
			if (parts.Length < 2) return;
			for (var i = 1; i < parts.Length; i++)
			{
				var action = parts[i];
				PerformAction(action);
			}
			e.Cancel = true;
			var url = parts[0].Replace("zerok://", String.Empty);
			WebBrowser.Navigate(url);
		}

		private void webBrowser_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
			WebBrowser.Source = new Uri("http://zero-k.info/Missions.mvc");
		}

		public void FocusWeb()
		{
			var document = (mshtml.HTMLDocument)WebBrowser.Document;
			document.focus();
		}
	}
}