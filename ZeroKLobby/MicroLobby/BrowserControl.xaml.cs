using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Navigation;
using ZeroKLobby.Notifications;
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

		bool firstTime = true;


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
			Program.MainWindow.NotifySection.AddBar(new System.Windows.Controls.Label{ Content = "Not Implemented"});
		}

		public string PathHead { get { return "http://zero-k.info/Missions.mvc"; } }

		public bool TryNavigate(params string[] path)
		{
			var pathString = String.Join("/", path);
			if (!pathString.StartsWith(PathHead)) return false;
			if (WebBrowser.Source != null && pathString != WebBrowser.Source.OriginalString) WebBrowser.Navigate(pathString);
			return true;
		}

		public void Hilite(HiliteLevel level, params string[] path)
		{
			throw new NotImplementedException();
		}

		public string GetTooltip(params string[] path)
		{
			throw new NotImplementedException();
		}

		void WebBrowser_Navigated(object sender, NavigationEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
			if (firstTime)
			{
				firstTime = false;
			}
			else NavigationControl.Instance.Path = e.Uri.OriginalString;
		}

		void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
			if (firstTime)
			{
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
		}

		private void webBrowser_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
			WebBrowser.Source = new Uri("http://zero-k.info/Missions.mvc");
		}
	}
}