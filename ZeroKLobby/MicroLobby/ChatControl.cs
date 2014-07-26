using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using ZeroKLobby.Lines;
using ZkData;

namespace ZeroKLobby.MicroLobby
{
    public partial class ChatControl: UserControl
    {
        protected bool filtering;
        bool mouseIsDown;
        readonly PlayerListItem notResultsItem = new PlayerListItem { Title = "No match", SortCategory = 3 };
        protected List<PlayerListItem> playerListItems = new List<PlayerListItem>();
        readonly PlayerListItem searchResultsItem = new PlayerListItem { Title = "Search results", SortCategory = 1 };
        public bool CanLeave { get { return ChannelName != "Battle"; } }
        public static EventHandler<ChannelLineArgs> ChannelLineAdded = (sender, args) => { };
        public string ChannelName { get; set; }
        public GameInfo GameInfo { get; set; }
        public bool IsTopicVisible {
            get { return topicPanel.Visible; }
            set {
                //Note: topic window doesn't have listener to any resize event. This is minor issues.
                float height = topicBox.LineSize;
                height *= topicBox.TotalDisplayLines + 1;
                //height *= 1.1f;
                height += topicBox.Margin.Top + topicBox.Margin.Bottom;
                topicPanel.Height = (int)height;
                topicPanel.Visible = value;
                if (value) Program.Conf.Topics.Remove(ChannelName);
                else {
                    Channel channel;
                    if (Program.TasClient.JoinedChannels.TryGetValue(ChannelName, out channel)) Program.Conf.Topics[channel.Name] = channel.TopicSetDate;
                }
            }
        }
        public IEnumerable<PlayerListItem> PlayerListItems { get { return playerListItems; } }
        public event EventHandler<EventArgs<string>> ChatLine { add { sendBox.LineEntered += value; } remove { sendBox.LineEntered -= value; } }

        public ChatControl() {}

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public ChatControl(string name) {
            InitializeComponent();

            var isDesignMode = Process.GetCurrentProcess().ProcessName == "devenv"; // workaround for this.DesignMode not working in constructor
            if (isDesignMode) return;

            var extras = new BitmapButton();
            extras.Text = "Extras";
            extras.Click += (s, e) => { ContextMenus.GetChannelContextMenu(this).Show(extras, new Point(0, 0)); };
            ChatBox.Controls.Add(extras);

            playerBox.DrawMode = DrawMode.OwnerDrawVariable;
            playerBox.MeasureItem += (s, e) => { }; // needed for ListBox.OnMeasureItem
            playerBox.BackColor = Program.Conf.BgColor;
            playerBox.ForeColor = Program.Conf.TextColor;

            playerSearchBox.BackColor = Program.Conf.BgColor;
            playerSearchBox.ForeColor = Program.Conf.TextColor;

            ChatBox.Font = Program.Conf.ChatFont; //make sure this is done before HistoryManager adds text, or text becomes black.

            Name = name;
            ChannelName = name;
            if (!DesignMode) HistoryManager.InsertLastLines(ChannelName, ChatBox);
            playerBox.Sorted = true;
            var lookingGlass = new PictureBox { Width = 20, Height = 20, Image = ZklResources.search, SizeMode = PictureBoxSizeMode.CenterImage };
            searchBarContainer.Controls.Add(lookingGlass);
            Program.ToolTip.SetText(lookingGlass, "Enter name or country shortcut to find");

            Program.ToolTip.SetText(playerSearchBox, "Enter name or country shortcut to find");

            VisibleChanged += ChatControl_VisibleChanged;

            ChatBox.MouseUp += chatBox_MouseUp;
            ChatBox.MouseDown += chatBox_MouseDown;
            ChatBox.MouseMove += chatBox_MouseMove;
            ChatBox.FocusInputRequested += (s, e) => GoToSendBox();
            ChatBox.ChatBackgroundColor = TextColor.background; //same as Program.Conf.BgColor but TextWindow.cs need this.
            ChatBox.IRCForeColor = 14; //mirc grey. Unknown use

            Program.TasClient.ChannelUserAdded += client_ChannelUserAdded;
            Program.TasClient.ChannelUserRemoved += client_ChannelUserRemoved;
            Program.TasClient.UserStatusChanged += TasClient_UserStatusChanged;
            Program.TasClient.ChannelUsersAdded += TasClient_ChannelUsersAdded;
            Program.TasClient.Said += client_Said;
            Program.TasClient.UserRemoved += TasClient_UserRemoved;
            Program.TasClient.ChannelTopicChanged += TasClient_ChannelTopicChanged;
            Program.TasClient.HourChime += client_HourChime;

            Channel channel;
            Program.TasClient.JoinedChannels.TryGetValue(ChannelName, out channel);

            //Topic Box that displays over the channel
            topicBox.IRCForeColor = 14; //mirc grey. Unknown use
            topicBox.ChatBackgroundColor = TextColor.topicBackground;
            topicBox.HorizontalScroll.Enabled = true;
            topicBox.BorderStyle = BorderStyle.FixedSingle;
            topicBox.VerticalScroll.Visible = false;
            topicBox.VerticalScroll.Enabled = false;
            topicBox.AutoSize = true;
            topicBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            topicBox.HideScroll = true;
            topicBox.ShowUnreadLine = false;
            topicBox.ShowHistory = false;

            //hide mappanel for normal chat operation. Overriden in BattleChatControl.cs 
            playerListMapSplitContainer.Panel2Collapsed = true;

            sendBox.CompleteWord += (word) => //autocomplete of username
                {
                    var w = word.ToLower();
                    IEnumerable <string> firstResult = playerBox.GetUserNames()
                        .Where(x => x.ToLower().StartsWith(w))
                        .Union(playerBox.GetUserNames().Where(x => x.ToLower().Contains(w)));
                    if (true)
                    {
                        ChatControl zkChatArea = Program.MainWindow.navigationControl.ChatTab.GetChannelControl("zk");
                        if (zkChatArea != null)
                        {
                            IEnumerable<string> extraResult = zkChatArea.playerBox.GetUserNames()
                                .Where(x => x.ToLower().StartsWith(w))
                                .Union(zkChatArea.playerBox.GetUserNames().Where(x => x.ToLower().Contains(w)));
                            firstResult = firstResult.Concat(extraResult); //Reference: http://stackoverflow.com/questions/590991/merging-two-ienumerablets
                        }
                    }
                    return firstResult;
                        
                };

            if (channel != null) foreach (var userName in Program.TasClient.JoinedChannels[ChannelName].ChannelUsers) AddUser(userName);
        }

        public virtual void AddLine(IChatLine line) {
            if (ChannelName != "zkadmin" &&
                ((line is SaidLine && Program.Conf.IgnoredUsers.Contains(((SaidLine)line).AuthorName)) ||
                 (line is SaidExLine && Program.Conf.IgnoredUsers.Contains(((SaidExLine)line).AuthorName)))) return;
            ChatBox.AddLine(line);
            ChannelLineAdded(this, new ChannelLineArgs() { Channel = ChannelName, Line = line });
            HistoryManager.LogLine(ChannelName, line);
        }

        public void GoToSendBox() {
            sendBox.Focus();
        }


        public void RefreshUser(string userName) {
            if (PlayerListItems.Any(i => i.UserName == userName)) {
                SortByTeam();
                if (filtering) FilterPlayers();
            }
        }

        public virtual void Reset() {
            playerBox.Items.Clear();
            playerListItems.Clear();
            ChatBox.Text = String.Empty;
        }

        protected void AddUser(string userName) {
            Channel channel;
            if (Program.TasClient.JoinedChannels.TryGetValue(ChannelName, out channel)) {
                if (!channel.ChannelUsers.Contains(userName)) {
                    Trace.WriteLine("Trying to add a user to a channel he hasn't joined (" + ChannelName + "/" + userName + ").");
                    return;
                }
            }

            playerListItems.RemoveAll(u => u.UserName == userName);
            var item = new PlayerListItem { UserName = userName };
            playerListItems.Add(item);

            if (filtering) FilterPlayers();
            else {
                playerBox.Items.Remove(playerBox.Items.SingleOrDefault(u => u.UserName == userName));
                playerBox.Items.Add(item);
                SortByTeam();
            }
        }


        void FilterPlayers() {
            if (!filtering) return;
            playerBox.BeginUpdate();
            var words = playerSearchBox.Text.ToUpper().Split(' ');
            foreach (var playerListItem in PlayerListItems) {
                var user = playerListItem.User;
                var match = true;
                foreach (var iteratedWord in words) {
                    var word = iteratedWord;
                    var negation = false;
                    if (word.StartsWith("-")) {
                        negation = true;
                        word = word.Substring(1);
                    }

                    if (String.IsNullOrEmpty(word)) continue;

                    bool isSpecialWordMatch;
                    if (FilterSpecialWordCheck(user, word, out isSpecialWordMatch)) {
                        if ((!negation && !isSpecialWordMatch) || (negation && isSpecialWordMatch)) {
                            match = false;
                            break;
                        }
                    }
                    else {
                        var userNameFound = playerListItem.UserName.ToUpper().Contains(word);
                        var countryFound = user.Country.ToUpper() == word;
                        var countryNameFound = user.CountryName.ToUpper() == word;
                        var clanFound = user.Clan != null && user.Clan.ToUpper() == word;
                        var factionFound = user.Faction != null && user.Faction.ToUpper() == word;
                        if (!negation) {
                            if (!(userNameFound || countryFound || countryNameFound || clanFound || factionFound)) {
                                match = false;
                                break;
                            }
                        }
                        else {
                            if ((userNameFound || countryFound || countryNameFound || clanFound || factionFound)) {
                                match = false;
                                break;
                            }
                        }
                    }
                }
                if (match) {
                    playerListItem.SortCategory = 2;
                    playerListItem.IsGrayedOut = false;
                }
                else {
                    playerListItem.SortCategory = 4;
                    playerListItem.IsGrayedOut = true;
                }
            }
            playerBox.Sorted = false;
            playerBox.Sorted = true;
            playerBox.EndUpdate();
        }

        static bool FilterSpecialWordCheck(User user, string word, out bool isMatch) {
            switch (word) {
                case "BOT":
                    isMatch = user.IsBot;
                    return true;
                case "AFK":
                    isMatch = user.IsAway;
                    return true;
                case "ADMIN":
                    isMatch = user.IsAdmin || user.IsZeroKAdmin;
                    return true;
                case "INGAME":
                    isMatch = user.IsInGame;
                    return true;
                case "INBATTLE":
                    isMatch = user.IsInBattleRoom;
                    return true;
                case "FRIEND":
                    isMatch = Program.FriendManager.Friends.Any(x => x == user.Name);
                    return true;
            }

            isMatch = false;
            return false;
        }

        void QuickMatchTracker_PlayerQuickMatchChanged(object sender, EventArgs<string> e) {
            RefreshUser(e.Data);
        }

        protected void RemoveUser(string userName) {
            var item = playerListItems.SingleOrDefault(u => u.UserName == userName);
            playerListItems.Remove(item);
            playerBox.Items.Remove(item);
            if (filtering) FilterPlayers();
        }

        void ShowChatContextMenu(Point location) {
            var contextMenu = ContextMenus.GetChannelContextMenu(this);
            try {
                Program.ToolTip.Visible = false;
                contextMenu.Show(ChatBox, location);
            } catch (Exception ex) {
                Trace.TraceError("Error displaying tooltip:{0}", ex);
            } finally {
                Program.ToolTip.Visible = true;
            }
        }


        void ShowPlayerContextMenu(User user, Control control, Point location) {
            var contextMenu = ContextMenus.GetPlayerContextMenu(user, this is BattleChatControl);
            try {
                Program.ToolTip.Visible = false;
                contextMenu.Show(control, location);
            } catch (Exception ex) {
                Trace.TraceError("Error displaying tooltip:{0}", ex);
            } finally {
                Program.ToolTip.Visible = true;
            }
        }

        protected virtual void SortByTeam() {}

        void ChatControl_VisibleChanged(object sender, EventArgs e) {
            if (Visible) GoToSendBox();
            else ChatBox.ResetUnread();
        }


        void TasClient_ChannelTopicChanged(object sender, TasEventArgs e) {
            if (ChannelName == e.ServerParams[0]) {
                var channel = Program.TasClient.JoinedChannels[ChannelName];
                DateTime lastChange;
                Program.Conf.Topics.TryGetValue(channel.Name, out lastChange);
                var topicLine = new TopicLine(channel.Topic, channel.TopicSetBy, channel.TopicSetDate);
                topicBox.Reset();
                topicBox.AddLine(topicLine);
                if (channel.Topic != null && lastChange != channel.TopicSetDate) IsTopicVisible = true;
                else IsTopicVisible = false;
            }
        }

        void TasClient_ChannelUsersAdded(object sender, TasEventArgs e) {
            if (e.ServerParams[0] != ChannelName) return;
            
            String[] invalidName = new String[32];
            int invalidNameIndex = 0;
            foreach (var username in Program.TasClient.JoinedChannels[ChannelName].ChannelUsers)
            {
                try
                {
                    var user = Program.TasClient.ExistingUsers[username];
                }
                catch (System.Collections.Generic.KeyNotFoundException)
                {
                    Trace.TraceInformation("ChatControl ERROR: player \"{0}\" did not exist or wasn't notified using ADDUSER command. Ignoring this username.", username);
                    invalidName[invalidNameIndex] = username;
                    if (invalidNameIndex < 32) invalidNameIndex = invalidNameIndex + 1;
                }
            }
            for (int i = 0; i <= invalidNameIndex; i++)
            {
                Program.TasClient.JoinedChannels[ChannelName].ChannelUsers.Remove(invalidName[i]);
            }
            
            playerListItems = (from name in Program.TasClient.JoinedChannels[ChannelName].ChannelUsers
                               let user = Program.TasClient.ExistingUsers[name]
                               select new PlayerListItem { UserName = user.Name }).ToList();

            if (filtering) FilterPlayers();
            else {
                playerBox.AddItemRange(playerListItems);
            }
        }

        void TasClient_UserRemoved(object sender, TasEventArgs e) {
            var userName = e.ServerParams[0];
            if (PlayerListItems.Any(u => u.UserName == userName)) AddLine(new LeaveLine(userName, "User has disconnected."));
        }

        void TasClient_UserStatusChanged(object sender, TasEventArgs e) {
            RefreshUser(e.ServerParams[0]);
        }

        void chatBox_MouseDown(object sender, MouseEventArgs e) {
            mouseIsDown = true;
        }

        void chatBox_MouseMove(object sender, MouseEventArgs e) {
            if (mouseIsDown) return;
        }


        void chatBox_MouseUp(object sender, MouseEventArgs me) {
            mouseIsDown = false;
            var word = ChatBox.HoveredWord.TrimEnd();

            if (word != null) {
                var user = Program.TasClient.ExistingUsers.Values.SingleOrDefault(x => x.Name.ToString().ToUpper() == word.ToUpper());
                if (user != null) {
                    if (me.Button == MouseButtons.Right || !Program.Conf.LeftClickSelectsPlayer) {
                        ShowPlayerContextMenu(user, ChatBox, me.Location);
                        return;
                    }
                    else playerBox.SelectUser(word);
                }
            }

            if (me.Button == MouseButtons.Right) ShowChatContextMenu(me.Location);
        }

        protected virtual void client_ChannelUserAdded(object sender, TasEventArgs e) {
            var channelName = e.ServerParams[0];
            if (ChannelName != channelName) return;
            var userName = e.ServerParams[1];
            AddUser(userName);
            AddLine(new JoinLine(userName));
        }

        protected virtual void client_ChannelUserRemoved(object sender, TasEventArgs e) {
            var channelName = e.ServerParams[0];
            if (ChannelName != channelName) return;
            var userName = e.ServerParams[1];
            var reason = e.ServerParams[2];
            RemoveUser(userName);
            AddLine(new LeaveLine(userName, reason));
        }

        void client_HourChime(object sender, EventArgs e) {
            AddLine(new ChimeLine());
        }

        void client_Said(object sender, TasSayEventArgs e) {
            if (e.Channel != ChannelName) return;
            if (e.Origin == TasSayEventArgs.Origins.Player) {
                if (e.Place == TasSayEventArgs.Places.Channel) {
                    if (e.Text.Contains(Program.Conf.LobbyPlayerName) && e.UserName != GlobalConst.NightwatchName) Program.MainWindow.NotifyUser("chat/channel/" + e.Channel, string.Format("{0}: {1}", e.UserName, e.Text), false, true);

                    if (!e.IsEmote) AddLine(new SaidLine(e.UserName, e.Text));
                    else AddLine(new SaidExLine(e.UserName, e.Text));
                }
            }
            else if (e.Origin == TasSayEventArgs.Origins.Server && e.Place == TasSayEventArgs.Places.Channel) AddLine(new ChannelMessageLine(e.Text));
        }

        void hideButton_Click(object sender, EventArgs e) {
            IsTopicVisible = false;
        }

        void playerBox_DoubleClick(object sender, EventArgs e) {
            var playerListItem = playerBox.SelectedItem as PlayerListItem;
            if (playerListItem != null && playerListItem.User != null) NavigationControl.Instance.Path = "chat/user/" + playerListItem.User.Name;
        }

        void playerBox_MouseClick(object sender, MouseEventArgs e) {
            var item = playerBox.HoverItem;
            if (item != null && item.UserName != null) {
                playerBox.SelectedItem = item;
                if (item.User != null && !Program.Conf.LeftClickSelectsPlayer) ShowPlayerContextMenu(item.User, playerBox, e.Location);
            }
            //playerBox.ClearSelected();
        }

        void playerSearchBox_TextChanged(object sender, EventArgs e) {
            if (!String.IsNullOrEmpty(playerSearchBox.Text)) {
                filtering = true;
                if (!playerBox.Items.Contains(searchResultsItem)) playerBox.Items.Add(searchResultsItem);
                if (!playerBox.Items.Contains(notResultsItem)) playerBox.Items.Add(notResultsItem);
                FilterPlayers();
            }
            else {
                filtering = false;
                playerBox.BeginUpdate();
                playerBox.Items.Remove(searchResultsItem);
                playerBox.Items.Remove(notResultsItem);
                foreach (var item in playerListItems) {
                    item.SortCategory = 0;
                    item.IsGrayedOut = false;
                }
                SortByTeam();
                playerBox.Sorted = false;
                playerBox.Sorted = true;
                playerBox.EndUpdate();
            }
        }

        void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e) {
            OnResize(e); //OnResize(e) will be intercepted by BattleChatControl.cs & resize minimap.
        }

        void playerListMapSplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {
            OnResize(e); //OnResize(e) will be intercepted by BattleChatControl.cs & resize minimap.
        }

        public class ChannelLineArgs: EventArgs
        {
            public string Channel;
            public IChatLine Line;
        }
    }
}