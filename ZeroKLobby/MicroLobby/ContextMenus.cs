using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;

namespace ZeroKLobby.MicroLobby
{
	static class ContextMenus
	{
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		[SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
			MessageId = "System.Windows.Forms.Menu+MenuItemCollection.Add(System.String)")]
		public static ContextMenu GetBotContextMenu(string botName)
		{
			var contextMenu = new ContextMenu();
			try
			{
				var botStatus = Enumerable.Single<BotBattleStatus>(Program.TasClient.MyBattle.Bots, b => b.Name == botName);

				{
					var item = new MenuItem("Remove") { Enabled = botStatus.owner == Program.TasClient.UserName };
					item.Click += (s, e) => Program.TasClient.RemoveBot(botName);
					contextMenu.MenuItems.Add(item);
				}
				{
					var item = new MenuItem("Set Color") { Enabled = botStatus.owner == Program.TasClient.UserName };
					item.Click += (s, e) =>
						{
							var botColor = botStatus.TeamColorRGB;
							var colorDialog = new ColorDialog { Color = Color.FromArgb(botColor[0], botColor[1], botColor[2]), Site = Program.FormMain.ChatTab.Site };
							if (colorDialog.ShowDialog() == DialogResult.OK)
							{
								var newColor = (int)(MyCol)colorDialog.Color;
								Program.TasClient.UpdateBot(botName, botStatus, newColor);
							}
						};
					contextMenu.MenuItems.Add(item);
				}
				{
					var item = new MenuItem("Ally With") { Enabled = botStatus.owner == Program.TasClient.UserName };
					int freeAllyTeam;

					var existingTeams = GetExistingTeams(out freeAllyTeam).Where(t => t != botStatus.AllyNumber).Distinct();
					if (existingTeams.Any())
					{
						foreach (var allyTeam in existingTeams)
						{
							var at = allyTeam;
							if (allyTeam != botStatus.AllyNumber)
							{
								var subItem = new MenuItem("Join Team " + (allyTeam + 1));
								subItem.Click += (s, e) =>
									{
										var newStatus = botStatus.Clone();
										newStatus.AllyNumber = at;
										Program.TasClient.UpdateBot(botName, newStatus, botStatus.TeamColor);
									};
								item.MenuItems.Add(subItem);
							}
						}
						item.MenuItems.Add("-");
					}
					var newTeamItem = new MenuItem("New Team");
					newTeamItem.Click += (s, e) =>
						{
							var newStatus = botStatus.Clone();
							newStatus.AllyNumber = freeAllyTeam;
							Program.TasClient.UpdateBot(botName, newStatus, botStatus.TeamColor);
						};
					item.MenuItems.Add(newTeamItem);
					contextMenu.MenuItems.Add(item);
				}
			}
			catch (Exception e)
			{
				Trace.WriteLine("Error generating bot context menu: " + e);
			}
			return contextMenu;
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public static ContextMenu GetChannelContextMenu(ChatControl chatControl)
		{
			var contextMenu = new ContextMenu();
			try
			{
				var headerItem = new MenuItem("Channel - " + chatControl.ChannelName) { Enabled = false, DefaultItem = true };

				contextMenu.MenuItems.Add(headerItem);
				contextMenu.MenuItems.Add("-");

				var showTopic = new MenuItem("Show Topic Header") { Checked = chatControl.TopicBox.Visible };
				showTopic.Click += (s, e) =>
					{
						chatControl.TopicPanel.Visible = !chatControl.TopicPanel.Visible;
						showTopic.Checked = chatControl.TopicPanel.Visible;
					};
				contextMenu.MenuItems.Add(showTopic);

				if (!StartPage.GameList.Any(g => g.Channel == chatControl.ChannelName) && chatControl.ChannelName != "Battle")
				{
					var autoJoinItem = new MenuItem("Automatically Join Channel") { Checked = Program.AutoJoinManager.Channels.Contains(chatControl.ChannelName) };
					autoJoinItem.Click += (s, e) =>
						{
							if (autoJoinItem.Checked) Program.AutoJoinManager.Remove(chatControl.ChannelName);
							else Program.AutoJoinManager.Add(chatControl.ChannelName);
							autoJoinItem.Checked = !autoJoinItem.Checked;
						};
					contextMenu.MenuItems.Add(autoJoinItem);
				}

				var showJoinLeaveLines = new MenuItem("Show Join/Leave Lines") { Checked = chatControl.ChatBox.ShowJoinLeave };
				showJoinLeaveLines.Click += (s, e) => chatControl.ChatBox.ShowJoinLeave = !chatControl.ChatBox.ShowJoinLeave;
				contextMenu.MenuItems.Add(showJoinLeaveLines);

				var showHistoryLines = new MenuItem("Show Recent History") { Checked = chatControl.ChatBox.ShowHistory };
				showHistoryLines.Click += (s, e) => chatControl.ChatBox.ShowHistory = !chatControl.ChatBox.ShowHistory;
				contextMenu.MenuItems.Add(showHistoryLines);

				var historyItem = new MenuItem("Open History");
				historyItem.Click += (s, e) => HistoryManager.OpenHistory(chatControl.ChannelName);
				contextMenu.MenuItems.Add(historyItem);

				if (chatControl.CanLeave)
				{
					var leaveItem = new MenuItem("Leave Channel");
					leaveItem.Click += (s, e) => Program.TasClient.LeaveChannel(chatControl.ChannelName);
					contextMenu.MenuItems.Add(leaveItem);
				}

				contextMenu.MenuItems.Add("-");
				if (chatControl is BattleChatControl)
				{
					contextMenu.MenuItems.Add(GetShowOptions());
					contextMenu.MenuItems.Add(GetAddBotItem());
				}
			}
			catch (Exception e)
			{
				Trace.WriteLine("Error generating channel context menu: " + e);
			}
			return contextMenu;
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public static ContextMenu GetPlayerContextMenu(User user, bool isBattle)
		{
			var contextMenu = new ContextMenu();
			try
			{
				var headerItem = new MenuItem("Player - " + user.Name) { Enabled = false, DefaultItem = true }; // default is to make it appear bold
				contextMenu.MenuItems.Add(headerItem);

				if (user.Name != Program.TasClient.UserName)
				{
					contextMenu.MenuItems.Add("-");

					var pmItem = new MenuItem("Send Message");
					pmItem.Click += (s, e) => ActionHandler.OpenPrivateMessageChannel(user.Name);
					contextMenu.MenuItems.Add(pmItem);

					if (Program.FriendManager.Friends.Contains(user.Name))
					{
						var pinItem = new MenuItem("Unfriend");
						pinItem.Click += (s, e) => Program.FriendManager.RemoveFriend(user.Name);
						contextMenu.MenuItems.Add(pinItem);
					}
					else
					{
						var pinItem = new MenuItem("Friend");
						pinItem.Click += (s, e) => Program.FriendManager.AddFriend(user.Name);
						contextMenu.MenuItems.Add(pinItem);
					}

					var joinItem = new MenuItem("Join Same Battle") { Enabled = Program.TasClient.ExistingUsers[user.Name].IsInBattleRoom };
					joinItem.Click += (s, e) => ActionHandler.JoinPlayer(user.Name);
					contextMenu.MenuItems.Add(joinItem);

					var followItem = new MenuItem("Follow");
					followItem.Click += (s, e) => ActionHandler.FollowPlayer(user.Name);
					contextMenu.MenuItems.Add(followItem);

					var ignoreUser = new MenuItem("Ignore User") { Checked = Program.Conf.IgnoredUsers.Contains(user.Name) };
					ignoreUser.Click += (s, e) =>
						{
							ignoreUser.Checked = !ignoreUser.Checked;
							if (ignoreUser.Checked) Program.Conf.IgnoredUsers.Add(user.Name);
							else Program.Conf.IgnoredUsers.Remove(user.Name);
						};
					contextMenu.MenuItems.Add(ignoreUser);
				}

				if (Program.TasClient.MyBattle != null)
				{
					var battleStatus = Program.TasClient.MyBattle.Users.SingleOrDefault(u => u.Name == user.Name);
					var myStatus = Program.TasClient.MyBattleStatus;

					if (isBattle)
					{
						contextMenu.MenuItems.Add("-");

						if (user.Name != Program.TasClient.UserName)
						{
							var allyWith = new MenuItem("Ally")
							               { Enabled = !battleStatus.IsSpectator && (battleStatus.AllyNumber != myStatus.AllyNumber || myStatus.IsSpectator) };
							allyWith.Click += (s, e) => ActionHandler.JoinAllyTeam(battleStatus.AllyNumber);
							contextMenu.MenuItems.Add(allyWith);

							if (battleStatus.TeamNumber == myStatus.TeamNumber)
							{
								var unShareControl = new MenuItem("Unshare Command")
								                     { Enabled = !battleStatus.IsSpectator && !myStatus.IsSpectator && (myStatus.Name != battleStatus.Name) };
								unShareControl.Click += (s, e) => ActionHandler.Unshare();
								contextMenu.MenuItems.Add(unShareControl);
							}
							else
							{
								var shareControl = new MenuItem("Share Command")
								                   { Enabled = !battleStatus.IsSpectator && (battleStatus.TeamNumber != myStatus.TeamNumber || myStatus.IsSpectator) };
								shareControl.Click += (s, e) => ActionHandler.CommShare(battleStatus);
								contextMenu.MenuItems.Add(shareControl);
							}
						}

						var colorItem = new MenuItem("Select Color") { Enabled = Program.TasClient.UserName == user.Name && !myStatus.IsSpectator };
						colorItem.Click += (s, e) =>
							{
								if (Program.TasClient.MyBattle == null) return;
								var myColor = Program.TasClient.MyBattleStatus.TeamColorRGB;
								var colorDialog = new ColorDialog { Color = Color.FromArgb(myColor[0], myColor[1], myColor[2]) };
								colorDialog.Site = Program.FormMain.ChatTab.Site;
								if (colorDialog.ShowDialog() == DialogResult.OK)
								{
									var newColor = (int)(MyCol)colorDialog.Color;
									var newStatus = Program.TasClient.MyBattleStatus.Clone();
									newStatus.TeamColor = newColor;
									Program.TasClient.SendMyBattleStatus(newStatus);
								}
							};
						contextMenu.MenuItems.Add(colorItem);

						contextMenu.MenuItems.Add(GetSetAllyTeamItem(user));
						contextMenu.MenuItems.Add(GetSetTeamItem(user));

						contextMenu.MenuItems.Add("-");
						contextMenu.MenuItems.Add(GetShowOptions());
						contextMenu.MenuItems.Add(GetAddBotItem());
					}
				}
			}
			catch (Exception e)
			{
				Trace.WriteLine("Error generating player context menu: " + e);
			}
			return contextMenu;
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public static ContextMenu GetPrivateMessageContextMenu(PrivateMessageControl control)
		{
			var contextMenu = new ContextMenu();
			try
			{
				var headerItem = new MenuItem("Private Channel - " + control.UserName);

				headerItem.Enabled = false;
				headerItem.DefaultItem = true; //This is to make it appear bold
				contextMenu.MenuItems.Add(headerItem);
				contextMenu.MenuItems.Add("-");

				if (Program.FriendManager.Friends.Contains(control.UserName))
				{
					var pinItem = new MenuItem("Unfriend");
					pinItem.Click += (s, e) => Program.FriendManager.RemoveFriend(control.UserName);
					contextMenu.MenuItems.Add(pinItem);
				}
				else
				{
					var pinItem = new MenuItem("Friend");
					pinItem.Click += (s, e) => Program.FriendManager.AddFriend(control.UserName);
					contextMenu.MenuItems.Add(pinItem);
				}

				var isUserOnline = Program.TasClient.ExistingUsers.ContainsKey(control.UserName);

				var followItem = new MenuItem("Follow");
				followItem.Enabled = isUserOnline;
				followItem.Click += (s, e) => ActionHandler.FollowPlayer(control.UserName);
				contextMenu.MenuItems.Add(followItem);

				var joinItem = new MenuItem("Join Same Battle");
				joinItem.Enabled = isUserOnline && Program.TasClient.ExistingUsers[control.UserName].IsInBattleRoom;
				joinItem.Click += (s, e) => ActionHandler.JoinPlayer(control.UserName);
				contextMenu.MenuItems.Add(joinItem);

				contextMenu.MenuItems.Add("-");

				var showJoinLeaveLines = new MenuItem("Show Join/Leave Lines") { Checked = control.ChatBox.ShowJoinLeave };
				showJoinLeaveLines.Click += (s, e) => control.ChatBox.ShowJoinLeave = !control.ChatBox.ShowJoinLeave;
				contextMenu.MenuItems.Add(showJoinLeaveLines);

				var showHistoryLines = new MenuItem("Show Recent History") { Checked = control.ChatBox.ShowHistory };
				showHistoryLines.Click += (s, e) => control.ChatBox.ShowHistory = !control.ChatBox.ShowHistory;
				contextMenu.MenuItems.Add(showHistoryLines);

				var historyItem = new MenuItem("Open History");
				historyItem.Click += (s, e) => HistoryManager.OpenHistory(control.UserName);
				contextMenu.MenuItems.Add(historyItem);

				if (control.CanClose)
				{
					var closeItem = new MenuItem("Close");
					closeItem.Click += (s, e) => ActionHandler.CloseChannel(control.UserName);
					contextMenu.MenuItems.Add(closeItem);
				}
			}
			catch (Exception e)
			{
				Trace.WriteLine("Error generating channel context menu: " + e);
			}

			return contextMenu;
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		static MenuItem GetAddBotItem()
		{
			var enabled = Program.TasClient.MyBattle != null && Program.ModStore.Ais != null && Program.ModStore.Ais.Any();
			var addBotItem = new MenuItem("Add computer player (Bot)" + (enabled ? String.Empty : " (Loading)")) { Enabled = enabled };
			if (Program.ModStore.Ais != null)
			{
				foreach (var bot in Program.ModStore.Ais)
				{
					var item = new MenuItem(bot.Name);
					var b = bot;
					item.Click += (s, e) =>
						{
							var botNumber = Enumerable.Range(1, int.MaxValue).First(i => !Program.TasClient.MyBattle.Bots.Any(bt => bt.Name == "Bot_" + i));
							var botStatus = Program.TasClient.MyBattleStatus.Clone();
							// new team        	
							botStatus.TeamNumber =
								Enumerable.Range(0, TasClient.MaxTeams - 1).FirstOrDefault(x => !Program.TasClient.MyBattle.Users.Any(y => y.TeamNumber == x));
							//different alliance than player
							botStatus.AllyNumber = Enumerable.Range(0, TasClient.MaxAlliances - 1).FirstOrDefault(x => x != botStatus.AllyNumber);

							Program.TasClient.AddBot("Bot_" + botNumber, botStatus, (int)(MyCol)Color.White, b.ShortName);
						};
					addBotItem.MenuItems.Add(item);
				}
			}
			return addBotItem;
		}

		static List<int> GetExistingTeams(out int freeAllyTeam)
		{
			var nonSpecs = Program.TasClient.MyBattle.Users.Where(p => !p.IsSpectator);
			var existingTeams = nonSpecs.GroupBy(p => p.AllyNumber).Select(team => team.Key).ToList();
			var botTeams = Program.TasClient.MyBattle.Bots.Select(bot => bot.AllyNumber);
			existingTeams.AddRange(botTeams.ToArray());
			freeAllyTeam = Enumerable.Range(0, Int32.MaxValue).First(allyTeam => !existingTeams.Contains(allyTeam));
			return existingTeams;
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		static MenuItem GetSetAllyTeamItem(User user)
		{
			var setAllyTeamItem = new MenuItem("Select Team");

			if (Program.TasClient.MyBattle == null || user.Name != Program.TasClient.UserName) setAllyTeamItem.Enabled = false;
			else if (Program.TasClient.MyBattle != null)
			{
				int freeAllyTeam;

				foreach (var allyTeam in GetExistingTeams(out freeAllyTeam).Distinct())
				{
					var at = allyTeam;
					if (allyTeam != Program.TasClient.MyBattleStatus.AllyNumber)
					{
						var item = new MenuItem("Join Team " + (allyTeam + 1));
						item.Click += (s, e) => ActionHandler.JoinAllyTeam(at);
						setAllyTeamItem.MenuItems.Add(item);
					}
				}

				setAllyTeamItem.MenuItems.Add("-");

				var newTeamItem = new MenuItem("Start New Team");
				newTeamItem.Click += (s, e) => ActionHandler.JoinAllyTeam(freeAllyTeam);
				setAllyTeamItem.MenuItems.Add(newTeamItem);

				if (!Program.TasClient.MyBattleStatus.IsSpectator)
				{
					var specItem = new MenuItem("Spectate");
					specItem.Click += (s, e) => ActionHandler.Spectate();
					setAllyTeamItem.MenuItems.Add(specItem);
				}
			}

			return setAllyTeamItem;
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		static MenuItem GetSetTeamItem(User user)
		{
			var setTeamItem = new MenuItem("Select Command Sharing");
			if (Program.TasClient.MyBattle == null || user.Name != Program.TasClient.UserName) setTeamItem.Enabled = false;
			else if (Program.TasClient.MyBattle != null)
			{
				foreach (var battleStatus in Program.TasClient.MyBattle.Users)
				{
					if (battleStatus.IsSpectator) continue;
					if (battleStatus.Name == Program.TasClient.UserName) continue;
					var shareControl = new MenuItem("With " + battleStatus.Name);
					var status = battleStatus;
					shareControl.Click += (s, e) => ActionHandler.CommShare(status);
					setTeamItem.MenuItems.Add(shareControl);
				}

				if (!Program.TasClient.MyBattleStatus.IsSpectator &&
				    Program.TasClient.MyBattle.Users.Any(u => !u.IsSpectator && u.TeamNumber == Program.TasClient.MyBattleStatus.TeamNumber))
				{
					setTeamItem.MenuItems.Add("-");
					var unshareItem = new MenuItem("Do not Share");
					unshareItem.Click += (s, e) => ActionHandler.Unshare();
					setTeamItem.MenuItems.Add(unshareItem);
				}
			}

			return setTeamItem;
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		static MenuItem GetShowOptions()
		{
			var modOptions = new MenuItem("Show Game Options") { Enabled = Program.TasClient.MyBattle != null };
			modOptions.Click += (s, e) =>
				{
					var form = new Form { Width = 1000, Height = 300, Icon = Resources.ZkIcon, Text = "Game options (Non Changeable)" };
					var optionsControl = new ModOptionsControl { Dock = DockStyle.Fill };
					form.Controls.Add(optionsControl);
					Program.TasClient.BattleClosed += (s2, e2) =>
						{
							form.Close();
							form.Dispose();
							optionsControl.Dispose();
						};
					form.Show(Program.FormMain);
				};
			return modOptions;
		}
	}
}