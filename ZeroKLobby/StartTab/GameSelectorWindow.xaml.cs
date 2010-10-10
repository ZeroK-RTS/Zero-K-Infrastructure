using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PlasmaShared;
using SpringDownloader.MicroLobby;

namespace SpringDownloader.Common
{
	/// <summary>
	/// Interaction logic for UcGameSelector.xaml
	/// </summary>
	public partial class GameSelectorWindow: Window
	{
		public IEnumerable<GameInfo> Games { get { return lbGames.Items.OfType<GameInfo>(); } }
		public bool IsCheckingEnabled { get; set; }
		public GameInfo LastClickedGame { get; private set; }
		public event EventHandler<EventArgs<GameInfo>> GameClicked = delegate { };

		public GameSelectorWindow()
		{
			InitializeComponent();
		}

		public GameSelectorWindow(IEnumerable<GameInfo> gameInfos, bool isChecking): this()
		{
			lbGames.ItemsSource = gameInfos;
			IsCheckingEnabled = isChecking;
			spButtons.Visibility = isChecking ? Visibility.Visible : Visibility.Collapsed;
			if (!IsCheckingEnabled) lbTitle.Text = "Please select game";
		}

		void BtnClose_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		void BtnStart_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		void UcRedButton_Click(object sender, RoutedEventArgs e)
		{
			var ucr = (UcRedButton)sender;
			var gameInfo = (GameInfo)ucr.Tag;
			LastClickedGame = gameInfo;
			GameClicked(this, new EventArgs<GameInfo>(gameInfo));
			if (!IsCheckingEnabled) Close();
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

		void UserControl_Loaded(object sender, RoutedEventArgs e) {}
	}
}