using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ZeroKLobby.MicroLobby
{
	/// <summary>
	/// Interaction logic for MissionControl.xaml
	/// </summary>
	public partial class MissionControl : UserControl, INavigatable
	{
		public MissionControl()
		{
			InitializeComponent();
		}

		public string PathHead { get { return "http://zero-k.info/Missions.mvc"; } }
		public bool TryNavigate(params string[] path)
		{
			var pathString = String.Join("/", path);
			if (!pathString.StartsWith(PathHead)) return false;
			if (pathString != webBrowser.Source.ToString()) webBrowser.Source = new Uri(pathString);
			return true;
		}

		private void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
		{
			
		}

		private void WebBrowser_Navigated(object sender, NavigationEventArgs e)
		{
			NavigationControl.Instance.Path = e.Uri.ToString();
		}
	}
}
