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

		public string PathHead { get { return "missions"; } }
		public bool TryNavigate(params string[] path)
		{
			return path.Length > 0 && path[0] == PathHead;
		}
	}
}
