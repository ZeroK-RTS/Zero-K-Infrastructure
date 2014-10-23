using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using LobbyClient;
using ContextMenu = System.Windows.Forms.ContextMenu;
using PlasmaShared.UnitSyncLib;
using ZeroKLobby.MicroLobby.ExtrasTab;

namespace ZeroKLobby.MicroLobby
{
    static class ContextMenus
    {

		private static bool refreshSpringAi = true;
		private static List<Ai> springAi;
        private static void AddAIToTeam(string botShortName)
        {
            var botNumber = Enumerable.Range(1, int.MaxValue).First(i => !Program.TasClient.MyBattle.Bots.Any(
                                bt => bt.Name == "Bot_" + i));
            var botStatus = Program.TasClient.MyBattleStatus.Clone();
            // new team        	
            botStatus.TeamNumber = Enumerable.Range(0, TasClient.MaxTeams - 1).FirstOrDefault(
                                    x => !Program.TasClient.MyBattle.Users.Any(y => y.TeamNumber == x));
            //different alliance than player
            botStatus.AllyNumber = Enumerable.Range(0, TasClient.MaxAlliances - 1).FirstOrDefault(x => x != botStatus.AllyNumber);
            Program.TasClient.AddBot("Bot_" + botNumber, botStatus, (int)(ZeroKLobby.MicroLobby.MyCol)Color.White, botShortName);
        } 

        static MenuItem GetAddBot()
        {
            var enabled = Program.TasClient.MyBattle != null && Program.ModStore.Ais != null && Program.ModStore.Ais.Any();
            var addBotItem = new MenuItem("Add computer player (Bot)" + (enabled ? String.Empty : " (Loading)"))
                             { Visible = enabled };

            addBotItem.MenuItems.AddRange(GetAddBotItems());

            return addBotItem;
        }

        public static MenuItem[] GetAddBotItems()
        {
            var addBotItems = new List<MenuItem>();
            if (Program.ModStore.Ais != null)
            {
                foreach (var bot in Program.ModStore.Ais)
                {
                    var item = new MenuItem(string.Format("{0} ({1})", bot.ShortName, bot.Description));
                    var b = bot;
                    item.Click += (s, e) =>
                    {
                        AddAIToTeam(b.ShortName);
                    };
                    addBotItems.Add(item);
                }
            }

            addBotItems.Add(new MenuItem("-"));
            MenuItem item3 = new MenuItem("Spring AI");
            item3.Select += (s, e2) =>
            {
                if (item3.MenuItems.Count == 0)
                {
                    if (refreshSpringAi)
                        springAi = SkirmishControlTool.GetSpringAIs(Program.SpringPaths.UnitSyncDirectory); //note: used UnitSyncDirectory because its just same as Engine folder
                    refreshSpringAi = false;
                    MenuItem springAIitem;
                    for (int i = 0; i < springAi.Count; i++)
                    {
                        string version = springAi[i].Version; //for keeping reference of independent value inside item.Click event;
                        string shortname = springAi[i].ShortName;
                        springAIitem = new MenuItem(string.Format("{0} ({1})", shortname, version));
                        springAIitem.Click += (s1, e3) => { AddAIToTeam(shortname + "|" + version); };
                        item3.MenuItems.Add(springAIitem);
                    }
                    item3.MenuItems.Add("-");
                    springAIitem = new MenuItem("Refresh List");
                    springAIitem.Click += (s1, e3) =>
                    {
                        refreshSpringAi = true;
                    };
                    item3.MenuItems.Add(springAIitem);
                }
            };
            addBotItems.Add(item3);

            return addBotItems.ToArray();
        }

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
                    var item = new System.Windows.Forms.MenuItem("Remove") { Enabled = botStatus.owner == Program.TasClient.UserName };
                    item.Click += (s, e) => Program.TasClient.RemoveBot(botName);
                    contextMenu.MenuItems.Add(item);
                }
                {
                    var item = new System.Windows.Forms.MenuItem("Set Color") { Enabled = botStatus.owner == Program.TasClient.UserName };
                    item.Click += (s, e) =>
                        {
                            var botColor = botStatus.TeamColorRGB;
                            // fixme: set site
                            var colorDialog = new ColorDialog { Color = Color.FromArgb(botColor[0], botColor[1], botColor[2]) };
                            if (colorDialog.ShowDialog() == DialogResult.OK)
                            {
                                var newColor = (int)(MyCol)colorDialog.Color;
                                Program.TasClient.UpdateBot(botName, botStatus, newColor);
                            }
                        };
                    contextMenu.MenuItems.Add(item);
                }
                {
                    var item = new System.Windows.Forms.MenuItem("Ally With") { Enabled = botStatus.owner == Program.TasClient.UserName };
                    int freeAllyTeam;

                    var existingTeams = GetExistingTeams(out freeAllyTeam).Where(t => t != botStatus.AllyNumber).Distinct();
                    if (existingTeams.Any())
                    {
                        foreach (var allyTeam in existingTeams)
                        {
                            var at = allyTeam;
                            if (allyTeam != botStatus.AllyNumber)
                            {
                                var subItem = new System.Windows.Forms.MenuItem("Join Team " + (allyTeam + 1));
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
                    var newTeamItem = new System.Windows.Forms.MenuItem("New Team");
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


        public static ContextMenu GetChannelContextMenu(ChatControl chatControl)
        {
            var contextMenu = new ContextMenu();
            try
            {
                var headerItem = new System.Windows.Forms.MenuItem("Channel - " + chatControl.ChannelName) { Enabled = false, DefaultItem = true };

                contextMenu.MenuItems.Add(headerItem);
                contextMenu.MenuItems.Add("-");

                if (!(chatControl is BattleChatControl))
                {
                    var showTopic = new System.Windows.Forms.MenuItem("Show Topic Header") { Checked = chatControl.IsTopicVisible};
                    showTopic.Click += (s, e) =>
                        {
                            chatControl.IsTopicVisible = !chatControl.IsTopicVisible;
                            showTopic.Checked = chatControl.IsTopicVisible;
                        };
                    contextMenu.MenuItems.Add(showTopic);
                }

                if (chatControl.ChannelName != "Battle")
                {
                    var autoJoinItem = new System.Windows.Forms.MenuItem("Automatically Join Channel")
                                       { Checked = Program.AutoJoinManager.Channels.Contains(chatControl.ChannelName) };
                    autoJoinItem.Click += (s, e) =>
                        {
                            if (autoJoinItem.Checked) Program.AutoJoinManager.Remove(chatControl.ChannelName);
                            else Program.AutoJoinManager.Add(chatControl.ChannelName);
                            autoJoinItem.Checked = !autoJoinItem.Checked;
                        };
                    contextMenu.MenuItems.Add(autoJoinItem);
                }

                var showJoinLeaveLines = new System.Windows.Forms.MenuItem("Show Join/Leave Lines") { Checked = chatControl.ChatBox.ShowJoinLeave };
                showJoinLeaveLines.Click += (s, e) => chatControl.ChatBox.ShowJoinLeave = !chatControl.ChatBox.ShowJoinLeave;
                contextMenu.MenuItems.Add(showJoinLeaveLines);

                var showHistoryLines = new System.Windows.Forms.MenuItem("Show Recent History") { Checked = chatControl.ChatBox.ShowHistory };
                showHistoryLines.Click += (s, e) => chatControl.ChatBox.ShowHistory = !chatControl.ChatBox.ShowHistory;
                contextMenu.MenuItems.Add(showHistoryLines);

                var historyItem = new System.Windows.Forms.MenuItem("Open History");
                historyItem.Click += (s, e) => HistoryManager.OpenHistory(chatControl.ChannelName);
                contextMenu.MenuItems.Add(historyItem);

                if (chatControl.CanLeave)
                {
                    var leaveItem = new System.Windows.Forms.MenuItem("Leave Channel");
                    leaveItem.Click += (s, e) => Program.TasClient.LeaveChannel(chatControl.ChannelName);
                    contextMenu.MenuItems.Add(leaveItem);
                }
                contextMenu.MenuItems.Add("-");
                MenuItem textColoringMenu = new System.Windows.Forms.MenuItem("Compose a colored text");
                textColoringMenu.Click += (s, e) => { ActionHandler.ShowColoringPanel(chatControl.sendBox); };
                contextMenu.MenuItems.Add(textColoringMenu);
                MenuItem unicodeTranslator = new System.Windows.Forms.MenuItem("Get Unicode symbols");
                unicodeTranslator.Click += (s, e) => { ActionHandler.ShowUnicodeTranslator(); };
                contextMenu.MenuItems.Add(unicodeTranslator);

                if (chatControl is BattleChatControl)
                {
                    contextMenu.MenuItems.Add("-");
                    contextMenu.MenuItems.Add(GetShowGameOptionsItem());
                    contextMenu.MenuItems.Add(GetAddBot());
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
                var headerItem = new System.Windows.Forms.MenuItem("Player - " + user.Name) { Enabled = false, DefaultItem = true };
                    // default is to make it appear bold
                contextMenu.MenuItems.Add(headerItem);

                if (user.Name != Program.TasClient.UserName)
                {
                    contextMenu.MenuItems.Add("-");

                    var details = new MenuItem("Details");
                    details.Click += (s, e) => NavigationControl.Instance.Path = "http://zero-k.info/Users/LobbyDetail/" + user.LobbyID;
                    contextMenu.MenuItems.Add(details);

                    var pmItem = new MenuItem("Send Message");
                    pmItem.Click += (s, e) => NavigationControl.Instance.Path = "chat/user/" + user.Name;
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

                    var joinItem = new MenuItem("Join Same Battle")
                                   { Enabled = Program.TasClient.ExistingUsers[user.Name].IsInBattleRoom };
                    joinItem.Click += (s, e) => ActionHandler.JoinPlayer(user.Name);
                    contextMenu.MenuItems.Add(joinItem);

                    var ignoreUser = new MenuItem("Ignore User") { Checked = Program.Conf.IgnoredUsers.Contains(user.Name) };
                    ignoreUser.Click += (s, e) =>
                        {
                            ignoreUser.Checked = !ignoreUser.Checked;
                            if (ignoreUser.Checked) Program.Conf.IgnoredUsers.Add(user.Name);
                            else Program.Conf.IgnoredUsers.Remove(user.Name);
                        };
                    contextMenu.MenuItems.Add(ignoreUser);

                    var reportUser = new MenuItem("Report User");
                    reportUser.Click += (s, e) => NavigationControl.Instance.Path = "http://zero-k.info/Users/ReportToAdminFromLobby/" + user.Name;
                    contextMenu.MenuItems.Add(reportUser);
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
                                           {
                                               Enabled =
                                                   !battleStatus.IsSpectator &&
                                                   (battleStatus.AllyNumber != myStatus.AllyNumber || myStatus.IsSpectator)
                                           };
                            allyWith.Click += (s, e) => ActionHandler.JoinAllyTeam(battleStatus.AllyNumber);
                            contextMenu.MenuItems.Add(allyWith);
                        }

                        var colorItem = new MenuItem("Select Color")
                                        { Enabled = Program.TasClient.UserName == user.Name && !myStatus.IsSpectator };
                        colorItem.Click += (s, e) =>
                            {
                                if (Program.TasClient.MyBattle == null) return;
                                var myColor = Program.TasClient.MyBattleStatus.TeamColorRGB;
                                var colorDialog = new ColorDialog { Color = Color.FromArgb(myColor[0], myColor[1], myColor[2]) };
                                // fixme: set site
                                // colorDialog.Site = Program.MainWindow.ChatTab.Site;
                                if (colorDialog.ShowDialog() == DialogResult.OK)
                                {
                                    var newColor = (int)(MyCol)colorDialog.Color;
                                    Program.Conf.DefaultPlayerColorInt = newColor;
                                    Program.SaveConfig();
                                    var newStatus = Program.TasClient.MyBattleStatus.Clone();
                                    newStatus.TeamColor = newColor;
                                    Program.TasClient.SendMyBattleStatus(newStatus);
                                }
                            };
                        contextMenu.MenuItems.Add(colorItem);

                        contextMenu.MenuItems.Add(GetSetAllyTeam(user));

                        contextMenu.MenuItems.Add("-");
                        contextMenu.MenuItems.Add(GetShowGameOptionsItem());
                        contextMenu.MenuItems.Add(GetAddBot());
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Error generating player context menu: " + e);
            }
            return contextMenu;
        }

        // warning: a lot of duplication in GetPrivateMessageContextMenuWpf! change both at once! (temporary solution)

        // warning: a lot of duplication in GetPrivateMessageContextMenuWpf! change both at once!  (temporary solution)
        public static ContextMenu GetPrivateMessageContextMenu(PrivateMessageControl control)
        {
            var contextMenu = new ContextMenu();
            try
            {
                var headerItem = new System.Windows.Forms.MenuItem("Private Channel - " + control.UserName);

                headerItem.Enabled = false;
                headerItem.DefaultItem = true; //This is to make it appear bold
                contextMenu.MenuItems.Add(headerItem);
                contextMenu.MenuItems.Add("-");

                var details = new System.Windows.Forms.MenuItem("Details");
                details.Click += (s, e) => NavigationControl.Instance.Path = "http://zero-k.info/Users/LobbyDetail/" + control.UserName;
                contextMenu.MenuItems.Add(details);

                if (Program.FriendManager.Friends.Contains(control.UserName))
                {
                    var pinItem = new System.Windows.Forms.MenuItem("Unfriend");
                    pinItem.Click += (s, e) => Program.FriendManager.RemoveFriend(control.UserName);
                    contextMenu.MenuItems.Add(pinItem);
                }
                else
                {
                    var pinItem = new System.Windows.Forms.MenuItem("Friend");
                    pinItem.Click += (s, e) => Program.FriendManager.AddFriend(control.UserName);
                    contextMenu.MenuItems.Add(pinItem);
                }

                var isUserOnline = Program.TasClient.ExistingUsers.ContainsKey(control.UserName);

                var joinItem = new System.Windows.Forms.MenuItem("Join Same Battle");
                joinItem.Enabled = isUserOnline && Program.TasClient.ExistingUsers[control.UserName].IsInBattleRoom;
                joinItem.Click += (s, e) => ActionHandler.JoinPlayer(control.UserName);
                contextMenu.MenuItems.Add(joinItem);

                var reportUser = new System.Windows.Forms.MenuItem("Report User");
                reportUser.Click += (s, e) => NavigationControl.Instance.Path = "http://zero-k.info/Users/ReportToAdminFromLobby/" + control.UserName;
                contextMenu.MenuItems.Add(reportUser);

                contextMenu.MenuItems.Add("-");

                var showJoinLeaveLines = new System.Windows.Forms.MenuItem("Show Join/Leave Lines") { Checked = control.ChatBox.ShowJoinLeave };
                showJoinLeaveLines.Click += (s, e) => control.ChatBox.ShowJoinLeave = !control.ChatBox.ShowJoinLeave;
                contextMenu.MenuItems.Add(showJoinLeaveLines);

                var showHistoryLines = new System.Windows.Forms.MenuItem("Show Recent History") { Checked = control.ChatBox.ShowHistory };
                showHistoryLines.Click += (s, e) => control.ChatBox.ShowHistory = !control.ChatBox.ShowHistory;
                contextMenu.MenuItems.Add(showHistoryLines);

                var historyItem = new System.Windows.Forms.MenuItem("Open History");
                historyItem.Click += (s, e) => HistoryManager.OpenHistory(control.UserName);
                contextMenu.MenuItems.Add(historyItem);

                if (control.CanClose)
                {
                    var closeItem = new System.Windows.Forms.MenuItem("Close");
                    closeItem.Click += (s, e) => Program.MainWindow.ChatTab.ClosePrivateTab(control.UserName);
                    contextMenu.MenuItems.Add(closeItem);
                }

                contextMenu.MenuItems.Add("-");
                MenuItem textColoringMenu = new System.Windows.Forms.MenuItem("Compose a colored text");
                textColoringMenu.Click += (s, e) => { ActionHandler.ShowColoringPanel(control.sendBox); };
                contextMenu.MenuItems.Add(textColoringMenu);
                MenuItem unicodeTranslator = new System.Windows.Forms.MenuItem("Get Unicode symbols");
                unicodeTranslator.Click += (s, e) => { ActionHandler.ShowUnicodeTranslator(); };
                contextMenu.MenuItems.Add(unicodeTranslator);
            }
            catch (Exception e)
            {
                Trace.WriteLine("Error generating channel context menu: " + e);
            }

            return contextMenu;
        }

        public static ContextMenu GetBattleListContextMenu(Battle battle)
        {
            var contextMenu = new ContextMenu();
            try
            {
                if (battle.Users.Count <= 1) return contextMenu; // don't display if just 1 player (probably a bot)

                var headerItem = new MenuItem("Message Player");
                headerItem.Enabled = false;
                headerItem.DefaultItem = true; //This is to make it appear bold
                contextMenu.MenuItems.Add(headerItem);
                contextMenu.MenuItems.Add("-");

                foreach (var user in battle.Users)
                {
                    if (!user.LobbyUser.IsBot)
                    {
                        var item = new System.Windows.Forms.MenuItem(user.Name);
                        item.Click += (s, e) => NavigationControl.Instance.Path = "chat/user/" + user.Name;
                        contextMenu.MenuItems.Add(item);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Error generating battle context menu: " + e);
            }

            return contextMenu;
        }

        public static MenuItem GetShowGameOptionsItem()
        {
            var modOptions = new System.Windows.Forms.MenuItem("Change Game Options") { Enabled = Program.TasClient.MyBattle != null };
            modOptions.Click += (s, e) =>
                {
                    ShowGameOptions();
                };
            return modOptions;
        }

        public static void ShowGameOptions()
        {
            var form = new Form { Width = 1000, Height = 300, Icon = ZklResources.ZkIcon, Text = "Game options" };
            var optionsControl = new ModOptionsControl { Dock = DockStyle.Fill };
            form.Controls.Add(optionsControl);
            Program.TasClient.BattleClosed += (s2, e2) =>
            {
                form.Close();
                form.Dispose();
                optionsControl.Dispose();
            };
            form.Show(); //hack show Program.FormMain
        }

        public static List<int> GetExistingTeams(out int freeAllyTeam)
        {
            var nonSpecs = Program.TasClient.MyBattle.Users.Where(p => !p.IsSpectator);
            var existingTeams = nonSpecs.GroupBy(p => p.AllyNumber).Select(team => team.Key).ToList();
            var botTeams = Program.TasClient.MyBattle.Bots.Select(bot => bot.AllyNumber);
            existingTeams.AddRange(botTeams.ToArray());
            freeAllyTeam = Enumerable.Range(0, 100).First(allyTeam => !existingTeams.Contains(allyTeam));
            return existingTeams;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        static MenuItem GetSetAllyTeam(User user)
        {
            var setAllyTeamItem = new MenuItem("Select Team");

            if (Program.TasClient.MyBattle == null || user.Name != Program.TasClient.UserName) setAllyTeamItem.Enabled = false;
            else if (Program.TasClient.MyBattle != null)
            {
                setAllyTeamItem.MenuItems.AddRange(GetSetAllyItems());
            }

            return setAllyTeamItem;
        }

        public static MenuItem[] GetSetAllyItems()
        {
            var setAllyItems = new List<MenuItem>();
            int freeAllyTeam;

            foreach (var allyTeam in GetExistingTeams(out freeAllyTeam).Distinct())
            {
                var at = allyTeam;
                if (allyTeam != Program.TasClient.MyBattleStatus.AllyNumber)
                {
                    var item = new MenuItem("Join Team " + (allyTeam + 1));
                    item.Click += (s, e) => ActionHandler.JoinAllyTeam(at);
                    setAllyItems.Add(item);
                }
            }

            setAllyItems.Add(new MenuItem("-"));

            var newTeamItem = new MenuItem("Start New Team");
            newTeamItem.Click += (s, e) => ActionHandler.JoinAllyTeam(freeAllyTeam);
            setAllyItems.Add(newTeamItem);

            if (!Program.TasClient.MyBattleStatus.IsSpectator)
            {
                var specItem = new System.Windows.Forms.MenuItem("Spectate");
                specItem.Click += (s, e) => ActionHandler.Spectate();
                setAllyItems.Add(specItem);
            }

            return setAllyItems.ToArray();
        }

    }
}