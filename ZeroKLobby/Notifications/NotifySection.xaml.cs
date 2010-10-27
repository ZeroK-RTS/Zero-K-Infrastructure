using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;

namespace ZeroKLobby.Notifications
{
	/// <summary>
	/// Interaction logic for NotifyBox.xaml
	/// </summary>
	public partial class NotifySection: UserControl
	{
		public IEnumerable<INotifyBar> Bars { get { return stackPanel.Children.OfType<WindowsFormsHost>().Select(host => ((NotifyBarContainer)host.Child).BarContent); } }

		public NotifySection()
		{
			InitializeComponent();
		}

		public void AddBar(INotifyBar bar)
		{
			if (GetNotifyBarHost(bar) == null)
			{
				var barContainer = new NotifyBarContainer(bar);
				var host = new WindowsFormsHost { Height = barContainer.Height, Child = barContainer };
				stackPanel.Children.Add(host);
			}
		}

		public void AddBar(Control bar)
		{
			if (!stackPanel.Children.Contains(bar)) stackPanel.Children.Add(bar);
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

		WindowsFormsHost GetNotifyBarHost(INotifyBar bar)
		{
			return stackPanel.Children.OfType<WindowsFormsHost>().FirstOrDefault(host => ((NotifyBarContainer)host.Child).BarContent == bar);
		}

		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			new ScannerBar(Program.SpringScanner);
		}
	}
}