using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ZeroKLobby.Notifications
{
	/// <summary>
	/// Interaction logic for NotifyBox.xaml
	/// </summary>
	public partial class NotifySection : UserControl
	{
		public IEnumerable<INotifyBar> Bars
		{
			get { return stackPanel.Children.OfType<WindowsFormsHost>().Select(host => ((NotifyBarContainer)host.Child).BarContent); }
		}

		public void AddBar(INotifyBar bar)
		{
			if (GetNotifyBarHost(bar) != null)
			{
				stackPanel.Children.Add(new WindowsFormsHost { Child = new NotifyBarContainer(bar) });
			}
		}

		public void AddBar(Control bar)
		{
			if (!stackPanel.Children.Contains(bar)) stackPanel.Children.Add(bar);
		}

		WindowsFormsHost GetNotifyBarHost(INotifyBar bar)
		{
			return stackPanel.Children.OfType<WindowsFormsHost>().FirstOrDefault(host => ((NotifyBarContainer)host.Child).BarContent == bar);
		}

		public void RemoveBar(INotifyBar bar)
		{
			var host = GetNotifyBarHost(bar);
			if (host != null) stackPanel.Children.Remove(host);
		}

		public void RemoveBar(Control bar)
		{
			if (stackPanel.Children.Contains(bar)) stackPanel.Children.Remove(bar);
		}

		public NotifySection()
		{
			InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			new ScannerBar(Program.SpringScanner);
		}
	}
}
