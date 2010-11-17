using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using LobbyClient;
using PlasmaShared;
using ZeroKLobby.Lines;
using ZeroKLobby.Notifications;
using ZeroKLobby.ToolTips;
using ZkData;
using ContextMenu = System.Windows.Forms.ContextMenu;
using Control = System.Windows.Forms.Control;
using Image = System.Drawing.Image;
using MenuItem = System.Windows.Forms.MenuItem;
using UserControl = System.Windows.Controls.UserControl;

namespace ZeroKLobby.MicroLobby
{
	/// <summary>
	/// Interaction logic for ChatTab2.xaml
	/// </summary>
	public partial class ChatTab2: UserControl, INavigatable
	{
		BattleChatControl battleChatControl;
		string focusWhenJoin;

		public ChatTab2()
		{
			InitializeComponent();
			if (Process.GetCurrentProcess().ProcessName == "devenv") return; // detect design mode

			Program.TasClient.ChannelJoined += client_ChannelJoined;
			Program.TasClient.Said += client_Said;
			Program.TasClient.ConnectionLost += TasClient_ConnectionLost;
			Program.TasClient.ChannelLeft += TasClient_ChannelLeft;
			Program.TasClient.LoginAccepted += TasClient_LoginAccepted;
			Program.TasClient.UserRemoved += TasClient_UserRemoved;
			Program.TasClient.UserAdded += TasClient_UserAdded;
			Program.FriendManager.FriendAdded += FriendManager_FriendAdded;
			Program.FriendManager.FriendRemoved += FriendManager_FriendRemoved;
			Program.TasClient.ChannelJoinFailed += TasClient_ChannelJoinFailed;
			Program.TasClient.ChannelLeaving += TasClient_ChannelLeaving;
			Program.TasClient.ChannelForceLeave += TasClient_ChannelForceLeave;
			Program.TasClient.BattleForceQuit += TasClient_BattleForceQuit;

			foreach (var channel in Program.TasClient.JoinedChannels.Values.Where(c => !IsIgnoredChannel(c.Name))) CreateChannelControl(channel.Name);
		}

		public void AddTab(string name, string title, Control control, string toolTip, int sortImportance)
		{
			var host = new WindowsFormsHost { Child = control };
			var tabItem = new TabItem { Tag = name, Header = control, Content = host };
			tabControl.Items.Add(tabItem);
		}

		public void CloseTab(string key)
		{
			var tab = GetTab(key);
			if (tab != null)
			{
				tabControl.Items.Remove(tab);
				if (key == "Battle")
				{
					battleChatControl.Dispose();
					battleChatControl = null;
				}
			}
		}

		public PrivateMessageControl CreatePrivateMessageControl(string userName)
		{
			var pmControl = new PrivateMessageControl(userName) { Dock = DockStyle.Fill };
			var isFriend = Program.FriendManager.Friends.Contains(userName);
			User user;
			var isOffline = !Program.TasClient.ExistingUsers.TryGetValue(userName, out user);
			var icon = isOffline ? ZeroKLobby.Resources.Grayuser : TextImage.GetUserImage(userName);
			var contextMenu = new ContextMenu();
			if (!isFriend)
			{
				var closeButton = new MenuItem();
				closeButton.Click += (s, e) => CloseTab(userName);
				contextMenu.MenuItems.Add(closeButton);
			}
			AddTab(userName, userName, pmControl, ToolTipHandler.GetUserToolTipString(userName), 0);
			pmControl.ChatLine += (s, e) =>
				{
					if (Program.TasClient.IsLoggedIn)
					{
						if (Program.TasClient.ExistingUsers.ContainsKey(userName)) Program.TasClient.Say(TasClient.SayPlace.User, userName, e.Data, false);
						else
							Program.TasClient.Say(TasClient.SayPlace.User, GlobalConst.NightwatchName, string.Format("!pm {0} {1}", userName, e.Data), false);
								// send using PM
					}
				};
			return pmControl;
		}

		public ChatControl GetChannelControl(string key)
		{
			var host = GetTabWindowsFormsHost(key);
			if (host != null) return host.Child as ChatControl;
			return null;
		}

		public string GetNextTabPath()
		{
			// fixme
			return PathHead;
		}

		public string GetPrevTabPath()
		{
			// fixme
			return PathHead;
		}

		public TabItem GetTab(string key)
		{
			return tabControl.Items.Cast<TabItem>().SingleOrDefault(i => (string)i.Tag == key);
		}

		public static bool IsIgnoredChannel(string channelName)
		{
			return channelName == "quickmatch" || channelName == "quickmatching";
		}

		public void OpenChannel(string channelName)
		{
			if (GetChannelControl(channelName) != null) SelectTab(channelName);
			else
			{
				focusWhenJoin = channelName;
				Program.TasClient.JoinChannel(channelName);
			}
		}

		public void OpenPrivateMessageChannel(string userName)
		{
			if (GetPrivateMessageControl(userName) == null) CreatePrivateMessageControl(userName);
			SelectTab(userName);
		}

		public void SelectTab(string key)
		{
			var tab = GetTab(key);
			if (tab != null) tabControl.SelectedItem = tab;
		}

		void AddBattleControl()
		{
			if (battleChatControl == null || battleChatControl.IsDisposed) battleChatControl = new BattleChatControl { Dock = DockStyle.Fill };
			if (GetTab("Battle") == null) AddTab("Battle", "Battle", battleChatControl, "Current battle room", 3);
		}

		ChatControl CreateChannelControl(string channelName)
		{
			if (IsIgnoredChannel(channelName)) return null;
			var chatControl = new ChatControl(channelName) { Dock = DockStyle.Fill };
			var gameInfo = StartPage.GameList.FirstOrDefault(x => x.Channel == channelName);
			chatControl.GameInfo = gameInfo;
			if (gameInfo != null) AddTab(channelName, gameInfo.FullName, chatControl, null, 2);
			else AddTab(channelName, channelName, chatControl, null, 1);
			chatControl.ChatLine += (s, e) => Program.TasClient.Say(TasClient.SayPlace.Channel, channelName, e.Data, false);
			return chatControl;
		}

		PrivateMessageControl GetPrivateMessageControl(string key)
		{
			var host = GetTabWindowsFormsHost(key);
			if (host != null) return host.Child as PrivateMessageControl;
			return null;
		}

		WindowsFormsHost GetTabWindowsFormsHost(string key)
		{
			var tab = GetTab(key);
			if (tab != null) return tab.Content as WindowsFormsHost;
			return null;
		}

		void FriendManager_FriendAdded(object sender, EventArgs<string> e)
		{
			var userName = e.Data;
			CloseTab(userName);
			CreatePrivateMessageControl(userName);
			SelectTab(userName);
		}

		void FriendManager_FriendRemoved(object sender, EventArgs<string> e)
		{
			CloseTab(e.Data);
		}

		void client_ChannelJoined(object sender, TasEventArgs e)
		{
			var channelName = e.ServerParams[0];
			CreateChannelControl(channelName);
			if (focusWhenJoin == channelName)
			{
				SelectTab(channelName);
				focusWhenJoin = null;
			}
		}

		void client_Said(object sender, TasSayEventArgs e)
		{
			if (e.Origin == TasSayEventArgs.Origins.Player)
			{
				if (Program.Conf.IgnoredUsers.Contains(e.UserName)) return;

				if (e.Place == TasSayEventArgs.Places.Battle && !e.IsEmote && !Program.TasClient.MyUser.IsInGame) Program.MainWindow.NotifyUser("chat/battle", null);
				if (e.Place == TasSayEventArgs.Places.Channel && !IsIgnoredChannel(e.Channel)) Program.MainWindow.NotifyUser("chat/channel/" + e.Channel, null);
				else if (e.Place == TasSayEventArgs.Places.Normal)
				{
					var otherUserName = e.Origin == TasSayEventArgs.Origins.Player ? e.Channel : e.UserName;

					// support for offline pm and persistent channels 
					if (otherUserName == GlobalConst.NightwatchName && e.Text.StartsWith("!pm"))
					{
						// message received
						if (e.UserName == GlobalConst.NightwatchName)
						{
							var regex = Regex.Match(e.Text, "!pm\\|([^\\|]*)\\|([^\\|]+)\\|([^\\|]+)\\|([^\\|]+)");
							if (regex.Success)
							{
								var chan = regex.Groups[1].Value;
								var name = regex.Groups[2].Value;
								var time = DateTime.Parse(regex.Groups[3].Value, CultureInfo.InvariantCulture).ToLocalTime();
								var text = regex.Groups[4].Value;

								if (string.IsNullOrEmpty(chan))
								{
									var pmControl = GetPrivateMessageControl(name) ?? CreatePrivateMessageControl(name);
									pmControl.AddLine(new SaidLine(name, text, time));
									MainWindow.Instance.NotifyUser("chat/user/" + name, string.Format("{0}: {1}", name, text), false, true);
								}
								else
								{
									var chatControl = GetChannelControl(chan) ?? CreateChannelControl(chan);
									chatControl.AddLine(new SaidLine(name, text, time));
									Program.MainWindow.NotifyUser("chat/channel/" + chan, null);
								}
							}
							else Trace.TraceWarning("Incomprehensible Nightwatch message: {0}", e.Text);
						}
						else // message sent to nightwatch
						{
							var regex = Regex.Match(e.Text, "!pm ([^ ]+) (.*)");
							if (regex.Success)
							{
								var name = regex.Groups[1].Value;
								var text = regex.Groups[2].Value;
								var pmControl = GetPrivateMessageControl(name) ?? CreatePrivateMessageControl(name);
								pmControl.AddLine(new SaidLine(Program.Conf.LobbyPlayerName, text));
							}
						}
					}
					else
					{
						var pmControl = GetPrivateMessageControl(otherUserName) ?? CreatePrivateMessageControl(otherUserName);
						if (!e.IsEmote) pmControl.AddLine(new SaidLine(e.UserName, e.Text));
						else pmControl.AddLine(new SaidExLine(e.UserName, e.Text));
						if (e.UserName != Program.TasClient.MyUser.Name) MainWindow.Instance.NotifyUser("chat/user/" + otherUserName, string.Format("{0}: {1}", otherUserName, e.Text), false, true);
					}
				}
			}
			else if (e.Origin == TasSayEventArgs.Origins.Server &&
			         (e.Place == TasSayEventArgs.Places.Motd || e.Place == TasSayEventArgs.Places.MessageBox || e.Place == TasSayEventArgs.Places.Server ||
			          e.Place == TasSayEventArgs.Places.Broadcast)) Trace.TraceInformation("TASC: {0}", e.Text);
		}

		void TasClient_BattleForceQuit(object sender, EventArgs e)
		{
			WarningBar.DisplayWarning("You were kicked from battle");
		}


		void TasClient_ChannelForceLeave(object sender, TasEventArgs e)
		{
			var channelName = e.ServerParams[0];
			var userName = e.ServerParams[1];
			var reason = e.ServerParams[2];
			WarningBar.DisplayWarning("You have been kicked by " + userName + ".\r\nReason: " + reason);
			var chatControl = GetChannelControl(channelName);
			if (chatControl != null)
			{
				chatControl.Reset();
				chatControl.Dispose();
				CloseTab(channelName);
			}
		}


		void TasClient_ChannelJoinFailed(object sender, TasEventArgs e)
		{
			if (e.ServerParams[0].Contains("Already in the channel!")) return;
			WarningBar.DisplayWarning("Channel Joining Error - " + e.ServerParams[0]);
		}

		void TasClient_ChannelLeaving(object sender, CancelEventArgs<string> e)
		{
			var channelName = e.Data;
			if (StartPage.GameList.Any(g => g.Channel == channelName)) e.Cancel = true;
		}

		void TasClient_ChannelLeft(object sender, TasEventArgs e)
		{
			var channelName = e.ServerParams[0];
			var chatControl = GetChannelControl(channelName);
			chatControl.Reset();
			chatControl.Dispose();
			CloseTab(channelName);
		}

		void TasClient_ConnectionLost(object sender, TasEventArgs e)
		{
			// todo: dispose all tabs?
			tabControl.Items.Clear();
			AddBattleControl();
		}

		void TasClient_LoginAccepted(object sender, TasEventArgs e)
		{
			AddBattleControl();
			foreach (var friendName in Program.FriendManager.Friends) CreatePrivateMessageControl(friendName);
			foreach (var game in StartPage.GameList) Program.TasClient.JoinChannel(game.Channel);
			foreach (var channel in Program.AutoJoinManager.Channels) Program.TasClient.JoinChannel(channel, Program.AutoJoinManager.GetPassword(channel));
		}

		public void SetIcon(string tabName, Image icon)
		{
			// fixme
		}

		void TasClient_UserAdded(object sender, EventArgs<User> e)
		{
			var userName = e.Data.Name;
			var pmControl = GetPrivateMessageControl(userName);
			if (pmControl != null) SetIcon(userName, Program.FriendManager.Friends.Contains(userName) ? ZeroKLobby.Resources.Friend : TextImage.GetUserImage(userName));
		}

		void TasClient_UserRemoved(object sender, TasEventArgs e)
		{
			var userName = e.ServerParams[0];
			var pmControl = GetPrivateMessageControl(userName);
			if (pmControl != null) SetIcon(userName, ZeroKLobby.Resources.Grayuser);
		}

		public string PathHead { get { return "chat"; } }

		public bool TryNavigate(params string[] path)
		{
			if (path.Length == 0) return false;
			if (path[0] != PathHead) return false;
			if (path.Length == 2 && !String.IsNullOrEmpty(path[1]))
			{
				if (path[1] == "battle")
				{
					SelectTab("Battle");
				}
			}
			if (path.Length == 3 && !String.IsNullOrEmpty(path[1]) && !String.IsNullOrEmpty(path[2]))
			{
				var type = path[1];
				if (type == "user")
				{
					var userName = path[2];
					OpenPrivateMessageChannel(userName);

				}
				else if (type == "channel")
				{
					var channelName = path[2];
					OpenChannel(channelName);
				}
			}
			return true;
		}

		public bool SetHilite(string tabName, HiliteLevel level)
		{
			// fixme
			return false;
		}

		public bool Hilite(HiliteLevel level, params string[] path)
		{
			if (path.Length == 0) return false;
			if (path[0] != PathHead) return false;
			if (path.Length >= 2 && path[1] == "battle") return SetHilite("Battle", level);
			else if (path.Length >= 3)
			{
				SetHilite(path[2], level);
			}
			return false;
		}

		public string GetTooltip(params string[] path)
		{
			return null;
		}

		private void BattleTab_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			// todo: show battle context menu?
		}

		private void PrivateMessageTab_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var element = (FrameworkElement)sender;
			// ContextMenus.GetPrivateMessageContextMenuWpf((PrivateMessageControl)element.DataContext)
		}

	}
}