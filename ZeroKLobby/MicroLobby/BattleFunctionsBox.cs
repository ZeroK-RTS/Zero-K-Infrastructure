using LobbyClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby;
using ZeroKLobby.Lines;

namespace ZeroKLobby.MicroLobby
{
    public partial class BattleFunctionBox : UserControl
    {
        private BitmapButton btnAddAI;
        private BitmapButton btnChangeTeam;
        private BitmapButton btnGameOptions;
        private BitmapButton btnMapList;
        private TableLayoutPanel layoutPanel;

        public BattleFunctionBox()
        {
            InitializeComponent();
            Program.ToolTip.SetText(btnGameOptions, "List available map/mod-options");
            Program.ToolTip.SetText(btnMapList, "List featured map");
            Program.ToolTip.SetText(btnChangeTeam, "Create or move to new team");
            Program.ToolTip.SetText(btnAddAI, "Add AI to other team");
        }

        private void InitializeComponent()
        {
            this.btnGameOptions = new ZeroKLobby.BitmapButton();
            this.btnMapList = new ZeroKLobby.BitmapButton();
            this.btnChangeTeam = new ZeroKLobby.BitmapButton();
            this.btnAddAI = new ZeroKLobby.BitmapButton();
            this.layoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.layoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnGameOptions
            // 
            this.btnGameOptions.BackColor = System.Drawing.Color.Transparent;
            this.btnGameOptions.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.btnGameOptions.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnGameOptions.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGameOptions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGameOptions.ForeColor = System.Drawing.Color.White;
            this.btnGameOptions.Location = new System.Drawing.Point(0, 0);
            this.btnGameOptions.Margin = new System.Windows.Forms.Padding(0);
            this.btnGameOptions.Name = "btnGameOptions";
            this.btnGameOptions.Size = new System.Drawing.Size(60, 24);
            this.btnGameOptions.TabIndex = 18;
            this.btnGameOptions.Text = "Options";
            this.btnGameOptions.UseVisualStyleBackColor = true;
            this.btnGameOptions.Click += new System.EventHandler(this.btnGameOptions_Click);
            this.btnGameOptions.Dock = DockStyle.Fill;
            // 
            // btnMapList
            // 
            this.btnMapList.BackColor = System.Drawing.Color.Transparent;
            this.btnMapList.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.btnMapList.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnMapList.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMapList.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMapList.ForeColor = System.Drawing.Color.White;
            this.btnMapList.Location = new System.Drawing.Point(60, 0);
            this.btnMapList.Margin = new System.Windows.Forms.Padding(0);
            this.btnMapList.Name = "btnMapList";
            this.btnMapList.Size = new System.Drawing.Size(60, 24);
            this.btnMapList.TabIndex = 17;
            this.btnMapList.Text = "Maps";
            this.btnMapList.UseVisualStyleBackColor = true;
            this.btnMapList.Click += new System.EventHandler(this.btnMapList_Click);
            this.btnMapList.Dock = DockStyle.Fill;
            // 
            // btnChangeTeam
            // 
            this.btnChangeTeam.BackColor = System.Drawing.Color.Transparent;
            this.btnChangeTeam.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.btnChangeTeam.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnChangeTeam.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnChangeTeam.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnChangeTeam.ForeColor = System.Drawing.Color.White;
            this.btnChangeTeam.Location = new System.Drawing.Point(120, 0);
            this.btnChangeTeam.Margin = new System.Windows.Forms.Padding(0);
            this.btnChangeTeam.Name = "btnChangeTeam";
            this.btnChangeTeam.Size = new System.Drawing.Size(60, 24);
            this.btnChangeTeam.TabIndex = 16;
            this.btnChangeTeam.Text = "Teams";
            this.btnChangeTeam.UseVisualStyleBackColor = false;
            this.btnChangeTeam.Click += new System.EventHandler(this.changeTeamButton_Click);
            this.btnChangeTeam.Dock = DockStyle.Fill;
            // 
            // btnAddAI
            // 
            this.btnAddAI.BackColor = System.Drawing.Color.Transparent;
            this.btnAddAI.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.btnAddAI.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnAddAI.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAddAI.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddAI.ForeColor = System.Drawing.Color.White;
            this.btnAddAI.Location = new System.Drawing.Point(180, 0);
            this.btnAddAI.Margin = new System.Windows.Forms.Padding(0);
            this.btnAddAI.Name = "btnAddAI";
            this.btnAddAI.Size = new System.Drawing.Size(60, 24);
            this.btnAddAI.TabIndex = 15;
            this.btnAddAI.Text = "Add AI";
            this.btnAddAI.UseVisualStyleBackColor = true;
            this.btnAddAI.Click += new System.EventHandler(this.addAIButton_Click);
            this.btnAddAI.Dock = DockStyle.Fill;
            // 
            // layoutPanel
            // 
            this.layoutPanel.AutoSize = true;
            this.layoutPanel.ColumnCount = 4;
            this.layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0.25F));
            this.layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0.25F));
            this.layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0.25F));
            this.layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0.25F));
            this.layoutPanel.Controls.Add(this.btnGameOptions);
            this.layoutPanel.Controls.Add(this.btnMapList);
            this.layoutPanel.Controls.Add(this.btnChangeTeam);
            this.layoutPanel.Controls.Add(this.btnAddAI);
            this.layoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutPanel.Location = new System.Drawing.Point(0, 0);
            this.layoutPanel.Name = "layoutPanel";
            this.layoutPanel.RowCount = 1;
            this.layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.layoutPanel.Size = new System.Drawing.Size(480, 24);
            this.layoutPanel.TabIndex = 0;
            // 
            // BattleFunctionBox
            // 
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.layoutPanel);
            this.ForeColor = System.Drawing.Color.Transparent;
            this.Name = "BattleFunctionBox";
            this.Size = new System.Drawing.Size(600, 24);
            this.Dock = DockStyle.Bottom;
            this.layoutPanel.ResumeLayout(true);
            this.ResumeLayout(false);
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
                    item.Click += (s, e2) =>
                    {
                        var botNumber =
                            Enumerable.Range(1, int.MaxValue).First(i => !Program.TasClient.MyBattle.Bots.Any(bt => bt.Name == "Bot_" + i));
                        var botStatus = Program.TasClient.MyBattleStatus.Clone();
                        // new team        	
                        botStatus.TeamNumber =
                            Enumerable.Range(0, TasClient.MaxTeams - 1).FirstOrDefault(
                                x => !Program.TasClient.MyBattle.Users.Any(y => y.TeamNumber == x));
                        //different alliance than player
                        botStatus.AllyNumber = Enumerable.Range(0, TasClient.MaxAlliances - 1).FirstOrDefault(x => x != botStatus.AllyNumber);

                        Program.TasClient.AddBot("Bot_" + botNumber, botStatus, (int)(MyCol)Color.White, b.ShortName);
                    };
                    menu.MenuItems.Add(item);
                }
            }
            menu.Show(btnAddAI, new Point(0, 0));
        }

        private void changeTeamButton_Click(object sender, EventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            if (Program.TasClient.MyBattle != null)
            {
                int freeAllyTeam;

                foreach (var allyTeam in ContextMenus.GetExistingTeams(out freeAllyTeam).Distinct())
                {
                    var at = allyTeam;
                    if (allyTeam != Program.TasClient.MyBattleStatus.AllyNumber)
                    {
                        var item = new System.Windows.Forms.MenuItem("Join Team " + (allyTeam + 1));
                        item.Click += (s, e2) => ActionHandler.JoinAllyTeam(at);
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
            Program.MainWindow.navigationControl.Path= "http://zero-k.info/Maps";
        }

        private void btnGameOptions_Click(object sender, EventArgs e)
        {
            if (Program.TasClient.MyBattle == null)
            {
                // TODO
                return;
            }
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
    }

}