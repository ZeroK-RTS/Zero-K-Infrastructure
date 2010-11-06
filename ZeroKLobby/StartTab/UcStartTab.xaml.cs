using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PlasmaShared;
using ZeroKLobby.Common;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.StartTab
{
	/// <summary>
	/// Interaction logic for UcStartTab.xaml
	/// </summary>
	public partial class UcStartTab: UserControl
	{
		readonly List<GameInfo> shuffledGames;

		public UcStartTab()
		{
			InitializeComponent();
			shuffledGames = StartPage.GameList.Shuffle().ToList();
		}

		void PickGamesAndStartQuickMatching()
		{
			foreach (var game in shuffledGames) game.IsSelected = Program.Conf.SelectedGames.Contains(game.Shortcut);
			var window = new GameSelectorWindow(StartPage.GameList.Shuffle(), true);
			if (window.ShowDialog() == true)
			{
				foreach (var g in window.Games)
				{
					if (g.IsSelected) ActionHandler.SelectGame(g.Shortcut);
					else ActionHandler.DeselectGame(g.Shortcut);
				}
				ActionHandler.StartQuickMatching(window.Games.Where(x => x.IsSelected));
			}
		}

		void btnMp_Click(object sender, RoutedEventArgs e)
		{
			PickGamesAndStartQuickMatching();
		}

		void btnSettings_Click(object sender, RoutedEventArgs e)
		{
			Utils.SafeStart(Utils.MakePath(Path.GetDirectoryName(Program.SpringPaths.Executable), "springsettings.exe"));
		}

		void btnSp_Click(object sender, RoutedEventArgs e)
		{
			var window = new GameSelectorWindow(StartPage.GameList.Where(x => x.Profiles.Any()), false);
			window.ShowDialog();
			var game = window.LastClickedGame;
			if (game != null)
			{
				if (game.Profiles.Count > 1)
				{
					var spSel = new SinglePlayerSelectorWindow(game.Profiles);
					spSel.ShowDialog();
					var profile = spSel.LastClickedProfile;
					if (profile != null) ActionHandler.StartScriptMission(profile.Name);
				}
				else ActionHandler.StartScriptMission(game.Profiles.First().Name);
			}
		}

		void btnTutorial_Click(object sender, RoutedEventArgs e)
		{
			var gamesWithTutorial = StartPage.GameList.Where(x => !String.IsNullOrEmpty(x.Tutorial));
			var window = new GameSelectorWindow(gamesWithTutorial, false);
			window.ShowDialog();
			var game = window.LastClickedGame;
			if (game != null) Utils.OpenWeb(game.Tutorial);
		}
	}
}