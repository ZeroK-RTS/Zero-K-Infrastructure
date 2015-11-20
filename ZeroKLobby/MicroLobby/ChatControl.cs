using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using ZeroKLobby.Lines;
using ZkData;
using Timer = System.Timers.Timer;

namespace ZeroKLobby.MicroLobby
{
    public partial class ChatControl: UserControl
    {
        ZKLMouseClick playerBox_zklclick = new ZKLMouseClick();

        protected bool filtering; //playerList filter
        bool mouseIsDown;
        readonly PlayerListItem notResultsItem = new PlayerListItem { Title = "No match", SortCategory = (int)PlayerListItem.SortCats.SearchNoMatchTitle };
        protected List<PlayerListItem> playerListItems = new List<PlayerListItem>();
        readonly PlayerListItem searchResultsItem = new PlayerListItem { Title = "Search results", SortCategory = (int)PlayerListItem.SortCats.SearchTitle };
        
        public bool CanLeave { get { return ChannelName != "Battle"; } }
        public static EventHandler<ChannelLineArgs> ChannelLineAdded = (sender, args) => { };
        Timer minuteTimer;
        public string ChannelName { get; set; }
        
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
                    if (Program.TasClient.JoinedChannels.TryGetValue(ChannelName, out channel)) Program.Conf.Topics[channel.Name] = channel.Topic.SetDate;
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
            extras.Click += (s, e) => { 
                var contextMenu = ContextMenus.GetChannelContextMenu(this);
                contextMenu = LineDehighlighter(contextMenu, null);
                contextMenu.Show(extras, new Point(0, 0));
            };
            ChatBox.Controls.Add(extras);

            playerBox.DrawMode = DrawMode.OwnerDrawVariable;
            playerBox.MeasureItem += (s, e) => { }; // needed for ListBox.OnMeasureItem
            playerBox.BackColor = Program.Conf.BgColor;
            playerBox.ForeColor = Program.Conf.TextColor;
            playerBox_zklclick.AttachTo(playerBox);
            playerBox_zklclick.MouseClick += PlayerBox_MouseClick;

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
//            Program.TasClient.ChannelUsersAdded += TasClient_ChannelUsersAdded;
            Program.TasClient.Said += client_Said;
            Program.TasClient.UserRemoved += TasClient_UserRemoved;
            Program.TasClient.ChannelTopicChanged += TasClient_ChannelTopicChanged;
            
            
            
            
            Program.SteamHandler.Voice.UserStartsTalking += VoiceOnUserChanged;
            Program.SteamHandler.Voice.UserStopsTalking += VoiceOnUserChanged;
            Program.SteamHandler.Voice.UserVoiceEnabled += VoiceOnUserChanged;

            Channel channel;
            Program.TasClient.JoinedChannels.TryGetValue(ChannelName, out channel);


            minuteTimer = new Timer(60000) { AutoReset = true };
            minuteTimer.Elapsed += (s, e) => {
                if (DateTime.Now.Minute == 0 && this.IsHandleCreated && !this.IsDisposed) this.Invoke(new Action(() => AddLine(new ChimeLine())));
            };
            minuteTimer.Start();


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

            if (channel != null) foreach (var userName in Program.TasClient.JoinedChannels[ChannelName].Users.Keys) AddUser(userName);
        }

        void VoiceOnUserChanged(ulong steamID)
        {
            Program.MainWindow.InvokeFunc(() => {
                var user = Program.TasClient.ExistingUsers.Values.FirstOrDefault(x => x.SteamID == steamID);
                if (user != null) RefreshUser(user.Name);    
            });
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
                if (filtering) FilterPlayers(); else playerBox.Invalidate();
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
                if (!channel.Users.ContainsKey(userName)) {
                    Trace.WriteLine("Trying to add a user to a channel he hasn't joined (" + ChannelName + "/" + userName + ").");
                    return;
                }
            }

            playerListItems.RemoveAll(u => u.UserName == userName);
            var item = new PlayerListItem { UserName = userName };
            playerListItems.Add(item);

            playerBox.Items.Remove(playerBox.Items.SingleOrDefault(u => u.UserName == userName));
            playerBox.Items.Add(item);

            if (filtering) FilterPlayers(); else SortByTeam();
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
                        var countryNameFound = CountryNames.GetName(user.Country).ToUpper() == word;
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
                    playerListItem.SortCategory = (int)PlayerListItem.SortCats.SearchMatchedPlayer;
                    playerListItem.IsGrayedOut = false;
                }
                else {
                    playerListItem.SortCategory = (int)PlayerListItem.SortCats.SearchNoMatchPlayer;
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
                    isMatch = user.IsAdmin;
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


        protected void RemoveUser(string userName) {
            var item = playerListItems.SingleOrDefault(u => u.UserName == userName);
            playerListItems.Remove(item);
            playerBox.Items.Remove(item);
            if (filtering) FilterPlayers();
        }

        /// <summary>
        /// Grey out lines that do not contain the targeted text. Can be used by user to sort out who says what
        /// </summary>
        ContextMenu LineDehighlighter(ContextMenu cm, string word)
        {
            if (!string.IsNullOrWhiteSpace(word) || !string.IsNullOrWhiteSpace(ChatBox.LineHighlight))
            {
                cm.MenuItems.Add("-");
            }
            if (!string.IsNullOrWhiteSpace(ChatBox.LineHighlight))
            {
                var lineFilter = new System.Windows.Forms.MenuItem("Defocus Chatlines except: \"" + ChatBox.LineHighlight + "\"") { Checked = true};
                lineFilter.Click += (s, e) => { ChatBox.LineHighlight = null; };
                cm.MenuItems.Add(lineFilter);
            }
            if (ChatBox.LineHighlight!=word && !string.IsNullOrWhiteSpace(word))
            {
                var lineFilter = new System.Windows.Forms.MenuItem("Defocus Chatlines except: \"" + word + "\"") { Checked = false};
                lineFilter.Click += (s, e) => { ChatBox.LineHighlight = word; };
                cm.MenuItems.Add(lineFilter);
            }
            return cm;
        }
        
        void ShowChatContextMenu(Point location, string word = null) {
            var contextMenu = ContextMenus.GetChannelContextMenu(this);

            contextMenu = LineDehighlighter(contextMenu, word);

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
            
            contextMenu = LineDehighlighter(contextMenu, user.Name);
            
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


        void TasClient_ChannelTopicChanged(object sender, ChangeTopic changeTopic) {
            if (ChannelName == changeTopic.ChannelName) {
                var channel = Program.TasClient.JoinedChannels[ChannelName];
                DateTime? lastChange;
                Program.Conf.Topics.TryGetValue(channel.Name, out lastChange);
                var topicLine = new TopicLine(channel.Topic.Text, channel.Topic.SetBy, channel.Topic.SetDate);
                topicBox.Reset();
                topicBox.AddLine(topicLine);
                if (channel.Topic != null && lastChange != channel.Topic.SetDate) IsTopicVisible = true;
                else IsTopicVisible = false;
            }
        }


        void TasClient_UserRemoved(object sender, UserDisconnected e) {
            var userName = e.Name;
            if (PlayerListItems.Any(u => u.UserName == userName)) AddLine(new LeaveLine(userName, string.Format("User has disconnected ({0}).", e.Reason)));
        }

        void TasClient_UserStatusChanged(object sender, OldNewPair<User> oldNewPair) {
            RefreshUser(oldNewPair.New.Name);
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

            if (me.Button == MouseButtons.Right) 
                ShowChatContextMenu(me.Location,word);
        }

        protected virtual void client_ChannelUserAdded(object sender, ChannelUserInfo e) {
            var channelName = e.Channel.Name;
            if (ChannelName != channelName) return;

            // todo fix/simplify code .. does not need separate adduser and mass add
            if (e.Users.Count == 1) {
                AddLine(new JoinLine(e.Users.First().Name));
                AddUser(e.Users.First().Name);
            } else {
                foreach (var user in e.Users) {
                    if (!playerListItems.Any(x => x.UserName == user.Name)) {
                        playerListItems.Add(new PlayerListItem() { UserName = user.Name });
                    }

                }

                playerBox.AddItemRange(playerListItems.Where(x => !playerBox.Items.Any(y => y.UserName == x.UserName)).ToList());
                FilterPlayers();
            }

        }

        protected virtual void client_ChannelUserRemoved(object sender, ChannelUserRemovedInfo e) {
            var channelName = e.Channel.Name;
            if (ChannelName != channelName) return;
            var userName = e.User.Name;
            var reason = e.Reason;
            RemoveUser(userName);
            AddLine(new LeaveLine(userName, reason));
        }


        void client_Said(object sender, TasSayEventArgs e) {
            if (e.Channel != ChannelName) return;
            if (!string.IsNullOrEmpty(e.UserName)) {
                if (e.Place == SayPlace.Channel) {
                    if (e.Text.Contains(Program.Conf.LobbyPlayerName) && e.UserName != GlobalConst.NightwatchName) Program.MainWindow.NotifyUser("chat/channel/" + e.Channel, string.Format("{0}: {1}", e.UserName, e.Text), false, true);

                    if (!e.IsEmote) AddLine(new SaidLine(e.UserName, e.Text, e.Time));
                    else AddLine(new SaidExLine(e.UserName, e.Text, e.Time));
                }
            }
            else if (e.Place == SayPlace.Channel) AddLine(new ChannelMessageLine(e.Text));
        }

        void hideButton_Click(object sender, EventArgs e) {
            IsTopicVisible = false;
        }


        //using MouseUp because it allow the PlayerBox's "HoverItem" to show correct value when rapid clicking
        protected virtual void PlayerBox_MouseClick(object sender, MouseEventArgs mea) //from BattleChatControl
        {
            if (playerBox_zklclick.clickCount >= 2) 
            { //Double click
                var playerListItem = playerBox.SelectedItem as PlayerListItem;
                if (playerListItem != null && playerListItem.User != null)
                    NavigationControl.Instance.Path = "chat/user/" + playerListItem.User.Name;
            } else 
            {
                var item = playerBox.HoverItem;
                if (item != null && item.UserName != null) {
                    playerBox.SelectedItem = item;
                    if (item.User != null && !Program.Conf.LeftClickSelectsPlayer) ShowPlayerContextMenu(item.User, playerBox, mea.Location);
                }
                //playerBox.ClearSelected();
            }
        }

        void playerSearchBox_TextChanged(object sender, EventArgs e) {
            if (!String.IsNullOrEmpty(playerSearchBox.Text)) {
                filtering = true;
                if (!playerBox.Items.Contains(searchResultsItem))
                {
                    if (this is BattleChatControl)
                    { //strip out buttons and label
                        playerBox.Items.Clear();
                        playerBox.AddItemRange(playerListItems.Where(x => !playerBox.Items.Any(y => y.UserName == x.UserName)).ToList());
                    }
                    playerBox.Items.Add(searchResultsItem);
                    playerBox.Items.Add(notResultsItem);
                }
                FilterPlayers();
            }
            else {
                filtering = false;
                playerBox.BeginUpdate();
                playerBox.Items.Remove(searchResultsItem);
                playerBox.Items.Remove(notResultsItem);
                foreach (var item in playerListItems) {
                    item.SortCategory = (int)PlayerListItem.SortCats.Uncategorized;
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

        /// <summary>
        /// A reimplementation of regular MouseClick event using MouseDown & MouseUp pair which can be used
        /// for the specific aim of delaying a click event until MouseUp, or/and to read Right-click event for
        /// NET's Control which didn't allow them (such as ListBox), or/and to simply count successive clicks.
        /// </summary>
        public class ZKLMouseClick
        {
            public event EventHandler<MouseEventArgs> MouseClick = delegate { };
            public int clickCount = 0;

            bool isDown;
            long lastClick = DateTime.Now.Ticks;
            Point lastLocation = new Point(0,0);
            MouseButtons lastButton = MouseButtons.None;

            readonly int systemDoubleClickTime = SystemInformation.DoubleClickTime * 10000; //10,000 ticks is a milisecond . http://msdn.microsoft.com/en-us/library/system.datetime.ticks.aspx

            public void AttachTo(Control toListenTo)
            {
                toListenTo.MouseUp += MouseUp;
                toListenTo.MouseDown += MouseDown;
            }
                        
            bool IsDoubleClick(Point newLocation)
            {
                if( DateTime.Now.Ticks - lastClick <= systemDoubleClickTime &&
               	    lastLocation.X - newLocation.X < 10 &&
               	    lastLocation.Y - newLocation.Y < 10)
                {
               	    return true;
                }
                return false;
            }

            void UpdatePositionAndTime(Point newLocation)
            {
                lastClick = DateTime.Now.Ticks;
                lastLocation = newLocation;
            }

            void MouseDown(object sender, MouseEventArgs mea) 
            {
                if (!isDown) {
                    isDown= true;
                    lastButton = mea.Button;
                }
            }

            void MouseUp(object sender, MouseEventArgs mea) 
            {
                //check for mouseUp/Down pair
                if (!isDown) return;
                isDown = false;

                if (lastButton != mea.Button) return;
                lastButton = MouseButtons.None;

                if (IsDoubleClick(mea.Location))
                    clickCount = clickCount + 1;
                else 
                    clickCount = 1;

                UpdatePositionAndTime(mea.Location);

                MouseClick(sender, mea);
            }
        }
    }
}