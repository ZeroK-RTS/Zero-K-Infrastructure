using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using ZeroKLobby;
using ZeroKLobby.Lines;
using ZeroKLobby.Notifications;
using ZkData;

namespace ZeroKLobby.MicroLobby
{
    // fixme: what if a channel and a player have the same name
    public partial class ChatTab: UserControl, INavigatable
    {
        BattleChatControl battleChatControl;
        readonly ToolTabs toolTabs = new ToolTabs { Dock = DockStyle.Fill };
    	string focusWhenJoin;

  

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public ChatTab()
        {
            SuspendLayout();
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
            Program.TasClient.ChannelForceLeave += TasClient_ChannelForceLeave;
            Program.TasClient.BattleForceQuit += TasClient_BattleForceQuit;
            
            foreach (var channel in Program.TasClient.JoinedChannels.Values.Where(c => !IsIgnoredChannel(c.Name))) CreateChannelControl(channel.Name);
            toolTabs.SelectTab("Battle");
            ResumeLayout();
        }

        public void CloseTab(string key, PrivateMessageControl pmControl = null)
        {
            if (pmControl != null) pmControl.EndEncryption(); //dispose any open encryption if any.
            toolTabs.RemoveTab(key);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public PrivateMessageControl CreatePrivateMessageControl(string userName)
        {
            var pmControl = new PrivateMessageControl(userName) { Dock = DockStyle.Fill };
            var isFriend = Program.FriendManager.Friends.Contains(userName);
            User user;
            var isOffline = !Program.TasClient.ExistingUsers.TryGetValue(userName, out user);
            var icon = isOffline ? ZklResources.grayuser : TextImage.GetUserImage(userName);
            //Note: context menu options is added in ContextMenus.cs
            toolTabs.AddTab(userName, userName, pmControl, icon, ToolTipHandler.GetUserToolTipString(userName), 0);
            pmControl.ChatLine +=
                (s, e) => { if (Program.TasClient.IsLoggedIn)
                {
                    string textToSend = e.Data;
                    if (pmControl.encryptionState == PrivateMessageControl.isInEncryption)
                    {
                        textToSend = PrivateMessageControl.encryptionSign + pmControl.encryptionInstance.AESEncryptTo64Base(textToSend, false, pmControl.myEncryptMsgCount); //encrypted chat
                        pmControl.myEncryptMsgCount += 1;
                    }
                    if (Program.TasClient.ExistingUsers.ContainsKey(userName)) Program.TasClient.Say(TasClient.SayPlace.User, userName, textToSend, false);
                    else Program.TasClient.Say(TasClient.SayPlace.User, GlobalConst.NightwatchName, string.Format("!pm {0} {1}", userName, textToSend), false); // send using PM
                } };
            return pmControl;
        }

        public static bool IsIgnoredChannel(string channelName)
        {
            return false;
        }

		public void OpenChannel(string channelName)
		{
			if (GetChannelControl(channelName) != null) toolTabs.SelectTab(channelName);
			else
			{
				focusWhenJoin = channelName;
				Program.TasClient.JoinChannel(channelName);
			}
		}

        public void OpenPrivateMessageChannel(string userName)
        {
            if (GetPrivateMessageControl(userName) == null) CreatePrivateMessageControl(userName);
            toolTabs.SelectTab(userName);
        }

        public string GetNextTabPath()
        {
            return toolTabs.GetNextTabPath();
        }
        public string GetPrevTabPath()
        {
            return toolTabs.GetPrevTabPath();
        }

        

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        void AddBattleControl()
        {
            if (battleChatControl == null || battleChatControl.IsDisposed) battleChatControl = new BattleChatControl { Dock = DockStyle.Fill };
            if (toolTabs.GetTab("Battle") == null) toolTabs.AddTab("Battle", "Battle", battleChatControl, ZklResources.battle, "Current battle room", 3);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        ChatControl CreateChannelControl(string channelName)
        {
            if (IsIgnoredChannel(channelName)) return null;
            var existing = GetChannelControl(channelName);
            if (existing != null) return existing;
            var chatControl = new ChatControl(channelName) { Dock = DockStyle.Fill };
            var gameInfo = KnownGames.List.FirstOrDefault(x => x.Channel == channelName);

            if (gameInfo != null) toolTabs.AddTab(channelName, gameInfo.FullName, chatControl, ZklResources.game, null, 2);
            else toolTabs.AddTab(channelName, channelName, chatControl, ZklResources.chat, null, 1);
        	chatControl.ChatLine += (s, e) => Program.TasClient.Say(TasClient.SayPlace.Channel, channelName, e.Data, false);
            return chatControl;
        }

        public ChatControl GetChannelControl(string channelName)
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
			if (focusWhenJoin == channelName)
			{
				toolTabs.SelectTab(channelName);
				focusWhenJoin = null;
			}
        }

        void client_Said(object sender, TasSayEventArgs e)
        {
            if (e.Origin == TasSayEventArgs.Origins.Player)
            {
                if (Program.Conf.IgnoredUsers.Contains(e.UserName))
                {
                    return;
                }

								if (e.Place == TasSayEventArgs.Places.Battle && !e.IsEmote && !Program.TasClient.MyUser.IsInGame) Program.MainWindow.NotifyUser("chat/battle", null);
								if (e.Place == TasSayEventArgs.Places.Channel && !IsIgnoredChannel(e.Channel)) Program.MainWindow.NotifyUser("chat/channel/" +e.Channel, null);
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
                                            else
                                            {
                                                Trace.TraceWarning("Incomprehensible Nightwatch message: {0}", e.Text);
                                            }
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

                                        var pmControl = GetPrivateMessageControl(otherUserName);
                                        // block non friend messages 
                                        if (pmControl == null && Program.Conf.BlockNonFriendPm && !Program.FriendManager.Friends.Contains(otherUserName) && !Program.TasClient.ExistingUsers[e.UserName].IsBot)

                                        {
                                            if (e.UserName != Program.TasClient.UserName)
                                                Program.TasClient.Say(TasClient.SayPlace.User,
                                                                      otherUserName,
                                                                      "Sorry, I'm busy and do not receive messages. If you want to ask something, use #zk channel. If you have issue to report use http://code.google.com/p/zero-k/issues/list",
                                                                      false);
                                        }
                                        else
                                        {
                                            pmControl = pmControl ?? CreatePrivateMessageControl(otherUserName);
                                            if (!e.IsEmote) pmControl.AddLine(new SaidLine(e.UserName, e.Text));
                                            else pmControl.AddLine(new SaidExLine(e.UserName, e.Text));
                                            if (e.UserName != Program.TasClient.MyUser.Name)
                                            {
                                                MainWindow.Instance.NotifyUser("chat/user/" + otherUserName,
                                                                               string.Format("{0}: {1}", otherUserName, e.Text),
                                                                               false,
                                                                               true);
                                            }
                                        }
                                    }

                }
            }
            else if (e.Origin == TasSayEventArgs.Origins.Server &&
                     (e.Place == TasSayEventArgs.Places.Motd || e.Place == TasSayEventArgs.Places.MessageBox ||
                      e.Place == TasSayEventArgs.Places.Server || e.Place == TasSayEventArgs.Places.Broadcast)) Trace.TraceInformation("TASC: {0}", e.Text);
            if (e.Place == TasSayEventArgs.Places.Server || e.Place == TasSayEventArgs.Places.MessageBox || e.Place == TasSayEventArgs.Places.Broadcast) WarningBar.DisplayWarning(e.Text);
        }

        void FriendManager_FriendAdded(object sender, EventArgs<string> e)
        {
            var userName = e.Data;
            toolTabs.RemoveTab(userName);
            var pm = CreatePrivateMessageControl(userName);
            toolTabs.SelectTab(userName);
            if (ZeroKLobby.MainWindow.frdWindow != null)
            {
                MainWindow.frdWindow.clearItemlist();
                MainWindow.frdWindow.refreshItems();
            }
        }

        void FriendManager_FriendRemoved(object sender, EventArgs<string> e)
        {
            toolTabs.RemoveTab(e.Data);
            if (ZeroKLobby.MainWindow.frdWindow != null)
            {
                MainWindow.frdWindow.clearItemlist();
                MainWindow.frdWindow.refreshItems();
            }
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
            WarningBar.DisplayWarning("You have been kicked from chat channel " + channelName + " by " + userName + ".\r\nReason: " + reason);
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


        void TasClient_ChannelLeft(object sender, TasEventArgs e)
        {
            var channelName = e.ServerParams[0];
            var chatControl = GetChannelControl(channelName);
            if (chatControl != null)
            {
                chatControl.Reset();
                chatControl.Dispose();
            }
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
            foreach (var channel in Program.AutoJoinManager.Channels) Program.TasClient.JoinChannel(channel, Program.AutoJoinManager.GetPassword(channel));
            var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            if (!string.IsNullOrEmpty(lang)) Program.TasClient.JoinChannel(lang);
        }

        void TasClient_UserAdded(object sender, EventArgs<User> e)
        {
        	var userName = e.Data.Name;
            var pmControl = GetPrivateMessageControl(userName);
            if (pmControl != null) toolTabs.SetIcon(userName, Program.FriendManager.Friends.Contains(userName) ? ZklResources.friend : TextImage.GetUserImage(userName));
        }

        void TasClient_UserRemoved(object sender, TasEventArgs e)
        {
            var userName = e.ServerParams[0];
            var pmControl = GetPrivateMessageControl(userName);
            if (pmControl != null) toolTabs.SetIcon(userName, ZklResources.grayuser);
        }

		public string PathHead { get { return "chat"; } }

        public bool TryNavigate(params string[] path) //called by NavigationControl.cs when user press Navigation button or the URL button
    	{
			if (path.Length == 0) return false;
			if (path[0] != PathHead) return false;
			if (path.Length == 2 && !String.IsNullOrEmpty(path[1]))
			{
				if (path[1] == "battle")
				{
					toolTabs.SelectTab("Battle");
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
            //note: the path is set from ToolTabs.cs (to NavigationControl.cs) which happens when user pressed the channel's name.
    	}

    	public bool Hilite(HiliteLevel level, string pathString)
    	{
                var path = pathString.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
				if (path.Length == 0) return false;
				if (path[0] != PathHead) return false;
				if (path.Length >= 2 && path[1] == "battle") return toolTabs.SetHilite("Battle", level);
				else if (path.Length >= 3)
				{
					toolTabs.SetHilite(path[2], level);
				}
				return false;
    	}

    	public string GetTooltip(params string[] path)
    	{
    		return null;
    	}

        public void Reload() {
            
        }

        public bool CanReload { get { return false; } }

        public bool IsBusy { get { return false; } }

        private void ChatTab_Load(object sender, EventArgs e)
        {
 
        }
    }
}