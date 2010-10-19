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
using ZeroKLobby.LuaMgr;
using ZeroKLobby.MapDownloader;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
	/// <summary>
	/// Interaction logic for NavigationControl.xaml
	/// </summary>
	public partial class NavigationControl : UserControl
	{
		ChatTab chatControl;
		ServerTab serverTab;
		StartPage startPageControl;
		BattleListTab battleListContainer;
		HelpControl helpControl;
		LuaMgrTab luaMgrTab;
		DownloaderTab downloaderTab;
		SettingsTab settingsTab;
		Page chatPage;
		Page startPage;
		Page serverTabPage;
		Page battleListPage;
		Page helpPage;
		Page widgetsPage;
		Page downloaderPage;
		Page settingsPage;

		public ChatTab ChatTab { get { return chatControl; } }

		// todo: refresh settings config when selected


		Page CreatePage(System.Windows.Forms.Control control, string title)
		{
			return new Page { Content = new WindowsFormsHost { Child = control }, KeepAlive = true, Title = title};
		}

		public NavigationControl()
		{
			InitializeComponent();

			chatControl = new ChatTab();
			chatPage = CreatePage(chatControl, "Chat");

			startPageControl = new StartPage();
			startPage = CreatePage(startPageControl, "Start");

			serverTab = new ServerTab();
			serverTabPage = CreatePage(serverTab, "Server");

			battleListContainer = new BattleListTab();
			battleListPage = CreatePage(battleListContainer, "Battles");

			helpControl = new HelpControl();
			helpPage = CreatePage(helpControl, "Help");

			luaMgrTab = new LuaMgrTab();
			widgetsPage = CreatePage(luaMgrTab, "Widgets");

			downloaderTab = new DownloaderTab();
			downloaderPage = CreatePage(downloaderTab, "Downloader");

			settingsTab = new SettingsTab();
			settingsPage = CreatePage(settingsTab, "Settings");
		}

		private void StartPage_Click(object sender, RoutedEventArgs e)
		{
			frame.Navigate(startPage);
		}

		private void ChatPage_Click(object sender, RoutedEventArgs e)
		{
			frame.Navigate(chatPage);
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			frame.Navigate(startPage);
		}

		void BattleListPage_Click(object sender, RoutedEventArgs e)
		{
			frame.Navigate(battleListPage);
		}

		void HelpPage_Click(object sender, RoutedEventArgs e)
		{
			frame.Navigate(helpPage);
		}

		void WidgetsPage_Click(object sender, RoutedEventArgs e)
		{
			frame.Navigate(widgetsPage);
		}

		void SettingsPage_Click(object sender, RoutedEventArgs e)
		{
			frame.Navigate(settingsPage);
		}

		void ServerPage_Click(object sender, RoutedEventArgs e)
		{
			frame.Navigate(serverTabPage);
		}

		void FreedomPage_Click(object sender, RoutedEventArgs e)
		{
			frame.Navigate(new Uri("http://www.google.com"));
		}

		void DownloaderPage_Click(object sender, RoutedEventArgs e)
		{
			frame.Navigate(downloaderPage);
		}
	}
}
