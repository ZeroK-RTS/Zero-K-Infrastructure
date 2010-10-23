using System;
using System.Windows.Controls;
using System.Windows.Navigation;
using ZeroKLobby.ServiceReference;

namespace ZeroKLobby.MicroLobby
{
	/// <summary>
	/// Interaction logic for MissionControl.xaml
	/// </summary>
	public partial class MissionControl: UserControl, INavigatable
	{
		MissionServiceClient client = new MissionServiceClient();

		public MissionControl()
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

		void StartMission(int missionID) {}

		public string PathHead { get { return "http://zero-k.info/Missions.mvc"; } }

		public bool TryNavigate(params string[] path)
		{
			var pathString = String.Join("/", path);
			if (!pathString.StartsWith(PathHead)) return false;
			if (pathString != webBrowser.Source.ToString()) webBrowser.Source = new Uri(pathString);
			return true;
		}

		void WebBrowser_Navigated(object sender, NavigationEventArgs e)
		{
			NavigationControl.Instance.Path = e.Uri.ToString();
		}

		void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
		{
			var parts = e.Uri.ToString().Split('@');
			if (parts.Length <= 1) return;
			var url = parts[0];
			for (var i = 1; i < parts.Length; i++)
			{
				var action = parts[i];
				PerformAction(action);
			}
			e.Cancel = true;
			webBrowser.Source = new Uri(url);
		}
	}
}