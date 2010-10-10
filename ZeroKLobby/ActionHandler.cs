using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using JetBrains.Annotations;
using LobbyClient;
using PlasmaShared;
using SpringDownloader.MicroLobby;
using SpringDownloader.Notifications;

namespace SpringDownloader
{
	/// <summary>
	/// Central place for gui-centric actions invloving one or more controls
	/// </summary>
	public static class ActionHandler
	{
		public static void ChangeChatChannel(string name)
		{
			Program.FormMain.ChatTab.SelectChatTab(name);
			ChangeTab(Tab.Chat);
		}

		public static void ChangeChatToBattle()
		{
			Program.FormMain.ChatTab.SelectBattleChat();
			ChangeTab(Tab.Chat);
		}

		/// <summary>
		/// Changes user's desired spectator state of battle - does not actually send tasclient state change
		/// </summary>
		/// <param name="state">new desired state</param>
		/// <returns>true if change allowed</returns>
		public static bool ChangeDesiredSpectatorState(bool state)
		{
			return Program.BattleBar.ChangeDesiredSpectatorState(state);
		}

		public static void ChangeTab(Tab tab)
		{
			Program.FormMain.SelectTab(tab);
		}

		/// <summary>
		/// Closes a channel in the chat tab (any tab on the left)
		/// </summary>
		public static void CloseChannel(string key)
		{
			Program.FormMain.ChatTab.CloseTab(key);
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
			ChangeTab(Tab.Chat);
			ChangeChatToBattle();
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

		/// <summary>
		/// Join a channel and switch to it
		/// </summary>
		public static void JoinAndSwitch(string channelName)
		{
			if (Program.TasClient.JoinedChannels.ContainsKey(channelName)) ChangeChatChannel(channelName);
			else
			{
				EventHandler<TasEventArgs> joinHandler = null;
				joinHandler = ((s, e) =>
					{
						if (e.ServerParams[0] == channelName)
						{
							ChangeChatChannel(channelName);
							Program.TasClient.ChannelJoined -= joinHandler;
						}
					});
				Program.TasClient.ChannelJoined += joinHandler;
				Program.TasClient.JoinChannel(channelName);
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
					ChangeTab(Tab.Chat);
					ChangeChatToBattle();
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
		/// Opens a private message channel and switches to it
		/// </summary>
		public static void OpenPrivateMessageChannel(string userName)
		{
			Program.FormMain.ChatTab.OpenPrivateMessageChannel(userName);
			ChangeTab(Tab.Chat);
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
			Program.FormMain.DisplayLog();
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
			ChangeTab(Tab.Chat);
			ChangeChatToBattle();
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
	}
}