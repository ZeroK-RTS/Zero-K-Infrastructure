using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using JetBrains.Annotations;
using LobbyClient;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.Notifications;

namespace ZeroKLobby
{
	/// <summary>
	/// Central place for gui-centric actions invloving one or more controls
	/// </summary>
	public static class ActionHandler
	{

		/// <summary>
		/// Changes user's desired spectator state of battle - does not actually send tasclient state change
		/// </summary>
		/// <param name="state">new desired state</param>
		/// <returns>true if change allowed</returns>
		public static bool ChangeDesiredSpectatorState(bool state)
		{
			return Program.BattleBar.ChangeDesiredSpectatorState(state);
		}

		/// <summary>
		/// Closes a channel in the chat tab (any tab on the left)
		/// </summary>
		public static void CloseChannel(string key)
		{
			Program.MainWindow.ChatTab.CloseTab(key);
		}

		/// <summary>
		/// Make this client join a team (not same as allyteam)
		/// </summary>
		public static void CommShare([NotNull] UserBattleStatus withUser)
		{
			if (withUser == null) throw new ArgumentNullException("withUser");
			if (ChangeDesiredSpectatorState(false))
			{
				var newStatus = Program.TasClient.MyBattleStatus.Clone();
				Program.BattleBar.CommShareWith = withUser.Name;
				newStatus.TeamNumber = withUser.TeamNumber;
				newStatus.IsSpectator = false;
				Program.TasClient.SendMyBattleStatus(newStatus);
			}
		}


		/// <summary>
		/// Deselects game
		/// </summary>
		public static void DeselectGame(string gameName)
		{
			if (Program.Conf.SelectedGames.Contains(gameName)) Program.Conf.SelectedGames.RemoveAll(x=>x == gameName);
			Program.SaveConfig();
		}

		/// <summary>
		/// Starts following a player
		/// </summary>
		public static void FollowPlayer(string name)
		{
			Program.BattleBar.StartFollow(name);
			NavigationControl.Instance.Path = "chat/battle";
		}

		/// <summary>
		/// Hides the next PM that has a specific string as message
		/// </summary>
		public static void HidePM(string text)
		{
			EventHandler<CancelEventArgs<TasSayEventArgs>> hideMessage = null;
			hideMessage = (s, e) =>
				{
					if (e.Data.Place == TasSayEventArgs.Places.Normal && e.Data.Text == text)
					{
						e.Cancel = true;
						Program.TasClient.PreviewSaidPrivate -= hideMessage;
					}
				};
			Program.TasClient.PreviewSaidPrivate += hideMessage;
		}

		/// <summary>
		/// Make this client join an ally team, join a free team, and unspec
		/// </summary>
		/// <param name="allyTeam"></param>
		public static void JoinAllyTeam(int allyTeam)
		{
			if (ChangeDesiredSpectatorState(false))
			{
				var newStatus = Program.TasClient.MyBattleStatus.Clone();
				newStatus.AllyNumber = allyTeam;
				newStatus.TeamNumber = Program.TasClient.MyBattle.GetFreeTeamID(Program.TasClient.UserName);
				newStatus.IsSpectator = false;
				Program.TasClient.SendMyBattleStatus(newStatus);
			}
		}


		public static void JoinSlot(MissionSlot slot)
		{
			if (ChangeDesiredSpectatorState(false))
			{
				var newStatus = Program.TasClient.MyBattleStatus.Clone();
				newStatus.AllyNumber = slot.AllyID;
				newStatus.TeamNumber = slot.TeamID;
				newStatus.IsSpectator = false;
				newStatus.TeamColor = slot.Color;
				Program.TasClient.SendMyBattleStatus(newStatus);
			}
		}

		/// <summary>
		/// Joins battle manually
		/// </summary>
		public static void JoinBattle(int battleID, string password)
		{
			EventHandler<EventArgs<Battle>> battleJoinHandler = null;
			EventHandler<TasEventArgs> battleJoinFailedHandler = null;

			battleJoinHandler = ((s, e) =>
				{
					Program.TasClient.BattleJoined -= battleJoinHandler;
					Program.TasClient.JoinBattleFailed -= battleJoinFailedHandler;
					NavigationControl.Instance.Path = "chat/battle";
				});

			battleJoinFailedHandler = ((s, e) =>
				{
					Program.TasClient.BattleJoined -= battleJoinHandler;
					Program.TasClient.JoinBattleFailed -= battleJoinFailedHandler;
					MessageBox.Show(PlasmaShared.Utils.Glue(e.ServerParams.ToArray()), "Battle joining failed");
				});

			Program.TasClient.BattleJoined += battleJoinHandler;
			Program.TasClient.JoinBattleFailed += battleJoinFailedHandler;

			Program.BattleBar.StartManualBattle(battleID, password);
		}


		/// <summary>
		/// Joins same battle as player
		/// </summary>
		public static void JoinPlayer(string name)
		{
			var client = Program.TasClient;
			if (!client.IsLoggedIn) return;
			User user;
			if (client.ExistingUsers.TryGetValue(name, out user) && user.IsInBattleRoom)
			{
				var bat = client.ExistingBattles.Values.FirstOrDefault(x => x.Users.Any(y => y.Name == name));
				if (bat != null) JoinBattle(bat.BattleID, null);
			}
		}

		/// <summary>
		/// Selects new game
		/// </summary>
		public static void SelectGame(string gameName)
		{
			if (!Program.Conf.SelectedGames.Contains(gameName))
			{
				Program.Conf.SelectedGames.Add(gameName);
				Program.SaveConfig();
			}
		}

		/// <summary>
		/// Displays a window with the debug log
		/// </summary>
		public static void ShowLog()
		{
			Program.MainWindow.DisplayLog();
		}


		/// <summary>
		/// Set this client as spectator
		/// </summary>
		public static void Spectate()
		{
			if (ChangeDesiredSpectatorState(true))
			{
				var newStatus = Program.TasClient.MyBattleStatus.Clone();
				newStatus.IsSpectator = true;
				Program.TasClient.SendMyBattleStatus(newStatus);
			}
		}

		public static void StartQuickMatching(IEnumerable<GameInfo> games)
		{
			Program.BattleBar.StartQuickMatch(games);
			NavigationControl.Instance.Path = "chat/battle";
		}

		public static void StartSinglePlayer(SinglePlayerProfile profile)
		{
			SinglePlayerBar.DownloadAndStartMission(profile);
		}

		public static void StopBattle()
		{
			Program.BattleBar.Stop();
		}


		/// <summary>
		/// Unspec this client
		/// </summary>
		public static void UnSpec()
		{
			if (ChangeDesiredSpectatorState(false))
			{
				var newStatus = Program.TasClient.MyBattleStatus.Clone();
				newStatus.IsSpectator = false;
				Program.TasClient.SendMyBattleStatus(newStatus);
			}
		}

		/// <summary>
		/// Don't commashare
		/// </summary>
		public static void Unshare()
		{
			Program.BattleBar.CommShareWith = null;
			if (ChangeDesiredSpectatorState(false))
			{
				var newStatus = Program.TasClient.MyBattleStatus.Clone();
				newStatus.TeamNumber = Program.TasClient.MyBattle.GetFreeTeamID(Program.TasClient.UserName);
				newStatus.IsSpectator = false;
				Program.TasClient.SendMyBattleStatus(newStatus);
			}
		}

		public static void SpawnAutohost(string gameName,
		                          string battleTitle,
		                          string password,
		                          bool useManage,
		                          int minPlayers,
		                          int maxPlayers,
		                          int teams,
		                          IEnumerable<string> springieCommands)
		{
			var hostSpawnerName = SpringieCommand.GetHostSpawnerName(gameName);

			var spawnCommand = SpringieCommand.Spawn(gameName, battleTitle, password);

			var waitingBar = WarningBar.DisplayWarning("Waiting for AutoHost to start");

			EventHandler<CancelEventArgs<TasSayEventArgs>> joinGame = null;
			joinGame = (s, e) =>
				{
					if (e.Data.Place == TasSayEventArgs.Places.Normal && e.Data.Origin == TasSayEventArgs.Origins.Player && (e.Data.Text == spawnCommand.Reply))
					{
						e.Cancel = true;
						Program.NotifySection.RemoveBar(waitingBar);
						Program.TasClient.PreviewSaidPrivate -= joinGame;
						var myHostName = e.Data.UserName;
						var battle = Program.TasClient.ExistingBattles.Values.First(b => b.Founder == myHostName);
						if (useManage) SpringieCommand.Manage(minPlayers, maxPlayers, teams).SilentlyExcecute(myHostName);
						if (springieCommands != null) foreach (var command in springieCommands)
						{
							HidePM(command);
							Program.TasClient.Say(TasClient.SayPlace.User, myHostName, command, false);
						}
						Program.TasClient.JoinBattle(battle.BattleID, password);
						NavigationControl.Instance.Path = "chat/battle";
					}
				};

			Program.TasClient.PreviewSaidPrivate += joinGame;
			HidePM(spawnCommand.Command);
			Program.TasClient.Say(TasClient.SayPlace.User, hostSpawnerName, spawnCommand.Command, false);
		}

        /// <summary>
        /// Selects Next Button
        /// </summary>
        public static void NextButton()
        {
            Program.MainWindow.ChatTab.NextTab();
        }
        /// <summary>
        /// Selects Previous Button
        /// </summary>
        public static void PrevButton()
        {
            Program.MainWindow.ChatTab.PrevTab();
        }

	}
}