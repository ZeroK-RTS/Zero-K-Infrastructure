using LobbyClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby;
using ZeroKLobby.Lines;
using PlasmaShared.UnitSyncLib;
using ZeroKLobby.MicroLobby.ExtrasTab;

namespace ZeroKLobby.Controls
{
    public partial class MinimapFuncBox : UserControl
    {
        public MinimapFuncBox()
        {
            InitializeComponent();
            Program.ToolTip.SetText(btnGameOptions, "List available map/mod-options");
            Program.ToolTip.SetText(btnMapList, "List featured maps");
            Program.ToolTip.SetText(btnChangeTeam, "Create or move to new team");
            Program.ToolTip.SetText(btnAddAI, "Add AI to other team");
        }

        private bool refreshSpringAi = true;
        private List<Ai> springAi;
        private void AddAIToTeam(string botShortName)
        {
            var botNumber = Enumerable.Range(1, int.MaxValue).First(i => !Program.TasClient.MyBattle.Bots.Any(bt => bt.Name == "Bot_" + i));
            var botStatus = Program.TasClient.MyBattleStatus.Clone();
            // new team        	
            botStatus.TeamNumber = Enumerable.Range(0, TasClient.MaxTeams - 1).FirstOrDefault(
                                    x => !Program.TasClient.MyBattle.Users.Any(y => y.TeamNumber == x));
            //different alliance than player
            botStatus.AllyNumber = Enumerable.Range(0, TasClient.MaxAlliances - 1).FirstOrDefault(x => x != botStatus.AllyNumber);
            Program.TasClient.AddBot("Bot_" + botNumber, botStatus, (int)(ZeroKLobby.MicroLobby.MyCol)Color.White, botShortName);
        } 


        private void addAIButton_Click(object sender, EventArgs e)
        {
            var enabled = Program.TasClient.MyBattle != null && Program.ModStore.Ais != null && Program.ModStore.Ais.Any();
            ContextMenu menu = new ContextMenu();
            if (!enabled)
            {
                // TODO
                return;
            }
            if (Program.ModStore.Ais != null)
            {
                foreach (var bot in Program.ModStore.Ais)
                {
                    var item = new System.Windows.Forms.MenuItem(string.Format("{0} ({1})", bot.ShortName, bot.Description));
                    var b = bot;
                    item.Click += (s, e2) => { AddAIToTeam(bot.ShortName); };
                    menu.MenuItems.Add(item);
                }
            }
            menu.MenuItems.Add("-");

            MenuItem item3 = new System.Windows.Forms.MenuItem("Spring AI");
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
                            springAIitem = new System.Windows.Forms.MenuItem(string.Format("{0} ({1})", shortname, version));
                            springAIitem.Click += (s1, e3) => { AddAIToTeam(shortname + "|" + version); };
                            item3.MenuItems.Add(springAIitem);
                        }
                        item3.MenuItems.Add("-");
                        springAIitem = new System.Windows.Forms.MenuItem("Refresh List");
                        springAIitem.Click += (s1, e3) => {
                            refreshSpringAi = true;
                        };
                        item3.MenuItems.Add(springAIitem);
                    }
                };
            menu.MenuItems.Add(item3);

            menu.Show(btnAddAI, new Point(0, 0));
        }

        private void changeTeamButton_Click(object sender, EventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            if (Program.TasClient.MyBattle != null)
            {
                int freeAllyTeam;
                bool iAmSpectator = Program.TasClient.MyBattleStatus.IsSpectator;
                foreach (var allyTeam in ZeroKLobby.MicroLobby.ContextMenus.GetExistingTeams(out freeAllyTeam).Distinct())
                {
                    if (iAmSpectator || allyTeam != Program.TasClient.MyBattleStatus.AllyNumber)
                    {
                        var item = new System.Windows.Forms.MenuItem("Join Team " + (allyTeam + 1));
                        item.Click += (s, e2) => ActionHandler.JoinAllyTeam(allyTeam);
                        menu.MenuItems.Add(item);
                    }
                }

                menu.MenuItems.Add("-");

                var newTeamItem = new System.Windows.Forms.MenuItem("Start New Team");
                newTeamItem.Click += (s, e2) => ActionHandler.JoinAllyTeam(freeAllyTeam);
                menu.MenuItems.Add(newTeamItem);

                if (!Program.TasClient.MyBattleStatus.IsSpectator)
                {
                    var specItem = new System.Windows.Forms.MenuItem("Spectate");
                    specItem.Click += (s, e2) => ActionHandler.Spectate();
                    menu.MenuItems.Add(specItem);
                }
            }

            menu.Show(btnChangeTeam, new Point(0, 0));
        }

        private void btnMapList_Click(object sender, EventArgs e)
        {
            Program.MainWindow.navigationControl.Path = "http://zero-k.info/Maps";
        }

        private void btnGameOptions_Click(object sender, EventArgs e)
        {
            if (Program.TasClient.MyBattle == null)
            {
                // TODO
                return;
            }
            var form = new Form { Width = 1000, Height = 300, Icon = ZklResources.ZkIcon, Text = "Game options" };
            var optionsControl = new ZeroKLobby.MicroLobby.ModOptionsControl { Dock = DockStyle.Fill };
            form.Controls.Add(optionsControl);
            Program.TasClient.BattleClosed += (s2, e2) =>
            {
                form.Close();
                form.Dispose();
                optionsControl.Dispose();
            };
            form.Show(); //hack show Program.FormMain
        }
    }
}
