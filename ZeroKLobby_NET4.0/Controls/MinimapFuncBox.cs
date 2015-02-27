﻿using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby;
using ZkData;

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

        public bool QueueMode
        {
            set
            {
                if (value)
                {
                    btnGameOptions.Visible = false;
                    btnAddAI.Visible = false;
                    btnChangeTeam.Visible = false;
                }
                else
                {
                    btnGameOptions.Visible = true;
                    btnAddAI.Visible = true;
                    btnChangeTeam.Visible = true;
                }
            }
        }

        private void addAIButton_Click(object sender, EventArgs e)
        {
            var enabled = Program.TasClient.MyBattle != null; // && Program.ModStore.Ais != null && Program.ModStore.Ais.Any();
            ContextMenu menu = new ContextMenu();

            if (!enabled)
            {
                // TODO
                return;
            }

            menu.MenuItems.AddRange(ZeroKLobby.MicroLobby.ContextMenus.GetAddBotItems());
            menu.Show(btnAddAI, new Point(0, 0));
        }

        private void changeTeamButton_Click(object sender, EventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            if (Program.TasClient.MyBattle != null)
            {
                menu.MenuItems.AddRange(ZeroKLobby.MicroLobby.ContextMenus.GetSetAllyItems());
            }

            menu.Show(btnChangeTeam, new Point(0, 0));
        }

        private void btnMapList_Click(object sender, EventArgs e)
        {
            Program.MainWindow.navigationControl.Path = string.Format("{0}/Maps", GlobalConst.BaseSiteUrl);
        }

        private void btnGameOptions_Click(object sender, EventArgs e)
        {
            if (Program.TasClient.MyBattle == null)
            {
                // TODO
                return;
            }

            ZeroKLobby.MicroLobby.ContextMenus.ShowGameOptions();
        }
    }
}
