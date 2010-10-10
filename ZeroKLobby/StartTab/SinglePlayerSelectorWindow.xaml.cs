using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ZeroKLobby.Common;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.StartTab
{
	/// <summary>
	/// Interaction logic for SinglePlayerSelectorWindow.xaml
	/// </summary>
	public partial class SinglePlayerSelectorWindow: Window
	{
		public SinglePlayerProfile LastClickedProfile { get; private set; }

		public IEnumerable<SinglePlayerProfile> DesignData
		{
			get
			{
				return StartPage.GameList.First(x => x.Profiles.Count > 1).Profiles;
			}
		}

		public SinglePlayerSelectorWindow()
		{
			InitializeComponent();
			lbProfiles.ItemsSource = StartPage.GameList.First(x => x.Profiles.Count > 1).Profiles;
		}

		public SinglePlayerSelectorWindow(IEnumerable<SinglePlayerProfile> profiles) :this()
		{
			lbProfiles.ItemsSource = profiles;
		}

		void UcRedButton_Click(object sender, RoutedEventArgs e)
		{
			var ucr = (ButtonBase)sender;
			var profile = (SinglePlayerProfile)ucr.Tag;
			LastClickedProfile = profile;
			Close();
		}

		void UcRedButton_MouseEnter(object sender, MouseEventArgs e)
		{
			var ucr = (ButtonBase)sender;
			var profile = (SinglePlayerProfile)ucr.Tag;
			lbTooltip.Text = profile.Description;
		}

		void UcRedButton_MouseLeave(object sender, MouseEventArgs e)
		{
			lbTooltip.Text = "";
		}
	}
}