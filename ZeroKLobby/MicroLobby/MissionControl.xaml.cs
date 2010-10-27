using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using mshtml;
using ZeroKLobby.Notifications;
using ZeroKLobby.ServiceReference;

namespace ZeroKLobby.MicroLobby
{
	/// <summary>
	/// Interaction logic for MissionControl.xaml
	/// </summary>
	public partial class MissionControl: UserControl, INavigatable
	{
		MissionServiceClient client;

		public MissionControl()
		{
			InitializeComponent();
		}

		public void FocusWeb()
		{
			var document = (HTMLDocument)WebBrowser.Document;
			document.focus();
		}

		void PerformAction(string actionString)
		{
			if (!string.IsNullOrEmpty(actionString))
			{
				var idx = actionString.IndexOf(':');
				if (idx > -1 && actionString.Substring(0, idx) == "start_mission") StartMission(Uri.UnescapeDataString(actionString.Substring(idx+1)));
			}
		}

		void StartMission(string name)
		{
			var wind = new Window();
			wind.Content = new MissionBar(name);
			wind.Show();
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

		void client_GetMissionByIDCompleted(object sender, ServiceReference.GetMissionByIDCompletedEventArgs e) {}

		void webBrowser_Loaded(object sender, RoutedEventArgs e)
		{
			if (Process.GetCurrentProcess().ProcessName == "devenv") return;
			client = new MissionServiceClient();
			client.GetMissionByIDCompleted += client_GetMissionByIDCompleted;
			WebBrowser.Source = new Uri("http://zero-k.info/Missions.mvc");
		}
	}
}