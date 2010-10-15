using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using ZeroKLobby;
using ZeroKLobby.Lines;
using ZeroKLobby.Notifications;
using ZeroKLobby.ToolTips;

namespace ZeroKLobby.MicroLobby
{
    // fixme: what if a channel and a player have the same name
    public partial class ChatTab: UserControl
    {
        BattleChatControl battleChatControl;
        readonly ToolTabs toolTabs = new ToolTabs { Dock = DockStyle.Fill };

				public bool Hilite(string channel)
				{
					return toolTabs.Hilite(channel);
				}

				public bool Flash(string channel)
				{
					return toolTabs.Flash(channel);
				}


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public ChatTab()
        {
            InitializeComponent();
            if (Process.GetCurrentProcess().ProcessName == "devenv") return; // detect design mode, workaround for non-working this.DesignMode 
            Controls.Add(toolTabs);
            AddBattleControl();

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

            AddBattleControl();
            foreach (var channel in Program.TasClient.JoinedChannels.Values.Where(c => !IsIgnoredChannel(c.Name))) CreateChannelControl(channel.Name);
            toolTabs.SelectTab("Battle");
        }

        public void CloseTab(string key)
        {
            toolTabs.RemoveTab(key);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public PrivateMessageControl CreatePrivateMessageControl(string userName)
        {
            var pmControl = new PrivateMessageControl(userName) { Dock = DockStyle.Fill };
            var isFriend = Program.FriendManager.Friends.Contains(userName);
            User user;
            var isOffline = !Program.TasClient.ExistingUsers.TryGetValue(userName, out user);
						var icon = isOffline ? Resources.Grayuser : TextImage.GetUserImage(userName);
            var contextMenu = new ContextMenu();
            if (!isFriend)
            {
                var closeButton = new MenuItem();
                closeButton.Click += (s, e) => toolTabs.RemoveTab(userName);
                contextMenu.MenuItems.Add(closeButton);
            }
            toolTabs.AddTab(userName, userName, pmControl, icon, ToolTipHandler.GetUserToolTipString(userName), 0);
            pmControl.ChatLine +=
                (s, e) => { if (Program.TasClient.IsLoggedIn) Program.TasClient.Say(TasClient.SayPlace.User, userName, e.Data, false); };
            return pmControl;
        }

        public static bool IsIgnoredChannel(string channelName)
        {
            return channelName == "quickmatch" || channelName == "quickmatching";
        }

        public void OpenPrivateMessageChannel(string userName)
        {
            if (GetPrivateMessageControl(userName) == null) CreatePrivateMessageControl(userName);
            toolTabs.SelectTab(userName);
        }

        public void SelectBattleChat()
        {
            toolTabs.SelectTab("Battle");
        }

        public void SelectChatTab(string channel)
        {
            toolTabs.SelectTab(channel);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        void AddBattleControl()
        {
            if (battleChatControl == null || battleChatControl.IsDisposed) battleChatControl = new BattleChatControl { Dock = DockStyle.Fill };
            if (toolTabs.GetTab("Battle") == null) toolTabs.AddTab("Battle", "Battle", battleChatControl, Resources.Battle, "Current battle room", 3);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        ChatControl CreateChannelControl(string channelName)
        {
            if (IsIgnoredChannel(channelName)) return null;
            var chatControl = new ChatControl(channelName) { Dock = DockStyle.Fill };
            var gameInfo = StartPage.GameList.FirstOrDefault(x => x.Channel == channelName);

            if (gameInfo != null) toolTabs.AddTab(channelName, gameInfo.FullName, chatControl, Resources.Game, null, 2);
						else toolTabs.AddTab(channelName, channelName, chatControl, Resources.chat, null, 1);
            chatControl.ChatLine += (s, e) => Program.TasClient.Say(TasClient.SayPlace.Channel, channelName, e.Data, false);
            return chatControl;
        }

        ChatControl GetChannelControl(string channelName)
        {
            return toolTabs.GetTab(channelName) as ChatControl;
        }

        PrivateMessageControl GetPrivateMessageControl(string userName)
        {
            return toolTabs.GetTab(userName) as PrivateMessageControl;
        }


        void client_ChannelJoined(object sender, TasEventArgs e)
        {
            var channelName = e.ServerParams[0];
            CreateChannelControl(channelName);
        }

        void client_Said(object sender, TasSayEventArgs e)
        {
            if (e.Origin == TasSayEventArgs.Origins.Player)
            {
                if (Program.Conf.IgnoredUsers.Contains(e.UserName))
                {
                    return;
                }

								if (e.Place == TasSayEventArgs.Places.Battle) toolTabs.Hilite("Battle");
								if (e.Place == TasSayEventArgs.Places.Channel && !IsIgnoredChannel(e.Channel)) toolTabs.Hilite(e.Channel);
								else if (e.Place == TasSayEventArgs.Places.Normal)
                {
                    var otherUserName = e.Origin == TasSayEventArgs.Origins.Player ? e.Channel : e.UserName;
                    var pmControl = GetPrivateMessageControl(otherUserName) ?? CreatePrivateMessageControl(otherUserName);
                    if (!e.IsEmote) pmControl.AddLine(new SaidLine(e.UserName, e.Text));
                    else pmControl.AddLine(new SaidExLine(e.UserName, e.Text));
                    if (e.UserName != Program.TasClient.MyUser.Name)
                    {
                    	toolTabs.Hilite(otherUserName);
											FormMain.Instance.NotifyUser(string.Format("{0}: {1}", otherUserName, e.Text), false, true);
                    }
                }
            }
            else if (e.Origin == TasSayEventArgs.Origins.Server &&
                     (e.Place == TasSayEventArgs.Places.Motd || e.Place == TasSayEventArgs.Places.MessageBox ||
                      e.Place == TasSayEventArgs.Places.Server || e.Place == TasSayEventArgs.Places.Broadcast)) Trace.TraceInformation("TASC: {0}", e.Text);
        }

        void FriendManager_FriendAdded(object sender, EventArgs<string> e)
        {
            var userName = e.Data;
            toolTabs.RemoveTab(userName);
            CreatePrivateMessageControl(userName);
            toolTabs.SelectTab(userName);
        }

        void FriendManager_FriendRemoved(object sender, EventArgs<string> e)
        {
            toolTabs.RemoveTab(e.Data);
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
                toolTabs.RemoveTab(channelName);
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
            toolTabs.RemoveTab(channelName);
        }


        void TasClient_ConnectionLost(object sender, TasEventArgs e)
        {
            toolTabs.DisposeAllTabs();
            AddBattleControl();
        }

        void TasClient_LoginAccepted(object sender, TasEventArgs e)
        {
            AddBattleControl();
            foreach (var friendName in Program.FriendManager.Friends) CreatePrivateMessageControl(friendName);
            foreach (var game in StartPage.GameList) Program.TasClient.JoinChannel(game.Channel);
            foreach (var channel in Program.AutoJoinManager.Channels) Program.TasClient.JoinChannel(channel, Program.AutoJoinManager.GetPassword(channel));
        }

        void TasClient_UserAdded(object sender, TasEventArgs e)
        {
            var userName = e.ServerParams[0];
            var pmControl = GetPrivateMessageControl(userName);
            if (pmControl != null) toolTabs.SetIcon(userName, Program.FriendManager.Friends.Contains(userName) ? Resources.Friend : TextImage.GetUserImage(userName));
        }

        void TasClient_UserRemoved(object sender, TasEventArgs e)
        {
            var userName = e.ServerParams[0];
            var pmControl = GetPrivateMessageControl(userName);
						if (pmControl != null) toolTabs.SetIcon(userName, Resources.Grayuser);
        }
    }
}