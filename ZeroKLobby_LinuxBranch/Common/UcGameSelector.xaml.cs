using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PlasmaShared;
using SpringDownloader.MicroLobby;

namespace SpringDownloader.Common
{
	/// <summary>
	/// Interaction logic for UcGameSelector.xaml
	/// </summary>
	public partial class UcGameSelector: UserControl
	{
		public bool IsCheckingEnabled { get; set; }

		public event EventHandler<EventArgs<GameInfo>> GameClicked = delegate { };


		public IEnumerable<GameInfo> SelectedGames
		{
			get
			{
				return lbGames.Items.OfType<GameInfo>().Where(x => x.IsSelected);
			}
		}

		public UcGameSelector()
		{
			InitializeComponent();
			IsCheckingEnabled = false;
		}

		public UcGameSelector(IEnumerable<GameInfo> gameInfos, bool isChecking): this()
		{
			lbGames.ItemsSource = gameInfos;
			IsCheckingEnabled = isChecking;
		}

		void UcRedButton_Click(object sender, RoutedEventArgs e)
		{
			var ucr = (UcRedButton)sender;
			var gameInfo = (GameInfo)ucr.Tag;
			GameClicked(this, new EventArgs<GameInfo>(gameInfo));
		}

		void UcRedButton_MouseEnter(object sender, MouseEventArgs e)
		{
			var ucr = (UcRedButton)sender;
			var gameInfo = (GameInfo)ucr.Tag;
			lbTooltip.Text = gameInfo.Description;
		}

		void UcRedButton_MouseLeave(object sender, MouseEventArgs e)
		{
			lbTooltip.Text = "";
		}

		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			lbGames.ItemsSource = StartPage.GameList;
		}
	}
}