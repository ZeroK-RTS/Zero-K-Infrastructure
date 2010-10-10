using System.Windows;
using System.Windows.Controls;
using SpringDownloader.StartTab;

namespace SpringDownloader.Common
{
	/// <summary>
	/// Interaction logic for UcSinglePlayerBrowser.xaml
	/// </summary>
	public partial class UcSinglePlayerBrowser: UserControl
	{
		public UcSinglePlayerBrowser()
		{
			InitializeComponent();
		}

		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			lbProfiles.ItemsSource = SinglePlayerProfiles.Profiles;
		}
	}
}