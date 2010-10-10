using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SpringDownloader;

namespace SpringDownloader.MicroLobby
{
    public partial class HostDialog: Form
    {
        public string BattleTitle { get { return battleTitleBox.Text; } }

        public string GameName { get { return rapidTagBox.Text; } }

        public bool IsManageEnabled { get { return enableManageBox.Checked; } }
        public int MaxPlayers { get { return maxPlayersBar.Value; } }
        public int MinPlayers { get { return minPlayersBar.Value; } }
        public string Password { get { return passwordBox.Text; } }
        public string SpringieCommands { get { return springieCommandsBox.Text; } }
        public int Teams { get { return teamsBar.Value; } }

        public HostDialog(GameInfo defaultGame)
        {
            InitializeComponent();
            maxPlayersBar.ValueChanged += maxPlayersSlider_ValueChanged;
            minPlayersBar.ValueChanged += minPlayersBar_ValueChanged;
            teamsBar.ValueChanged += teamsBar_ValueChanged;
            gameBox.SelectedIndexChanged += gameBox_TextChanged;
            battleTitleBox.Text = Program.TasClient.MyUser + "'s Battle";
            HideAdvanced();
            gameBox.DropDownStyle = ComboBoxStyle.DropDownList;
            gameBox.Items.AddRange(StartPage.GameList.Select(g => g.FullName).ToArray());
            if (defaultGame == null) gameBox.SelectedIndex = new Random().Next(0, gameBox.Items.Count);
            else gameBox.SelectedIndex = gameBox.Items.IndexOf(gameBox.Items.Cast<string>().Single(n => n == defaultGame.FullName));
            rapidTagBox.Text = StartPage.GameList.Single(g => g.FullName == gameBox.Text).RapidTag;
            if (Program.Conf.HasHosted)
            {
                try
                {
                    maxPlayersBar.Value = Program.Conf.HostBattle_MaxPlayers;
                    minPlayersBar.Value = Program.Conf.HostBattle_MinPlayers;
                    teamsBar.Value = Program.Conf.HostBattle_Teams;
                    battleTitleBox.Text = Program.Conf.HostBattle_Title;
                    enableManageBox.Checked = Program.Conf.HostBattle_UseManage;
                    springieCommandsBox.Text = Program.Conf.HostBattle_SpringieCommands;
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Error in restoring battle configuration: " + e);
                }
            }
        }

        void HideAdvanced()
        {
            Height = 420;
            advancedOptionsGroup.Visible = false;
            showAdvancedButton.Text = "Show Advanced Options";
        }

        void ShowAdvanced()
        {
            Height = 590;
            advancedOptionsGroup.Visible = true;
            showAdvancedButton.Text = "Hide Advanced Options";
        }

        void enableManageBox_CheckedChanged(object sender, EventArgs e)
        {
            if (enableManageBox.Checked)
            {
                minPlayersBar.Enabled = true;
                maxPlayersBar.Enabled = true;
                teamsBar.Enabled = true;
            }
            else
            {
                minPlayersBar.Enabled = false;
                maxPlayersBar.Enabled = false;
                teamsBar.Enabled = false;
            }
        }

        void gameBox_TextChanged(object sender, EventArgs e)
        {
            rapidTagBox.Text = StartPage.GameList.Single(g => g.FullName == gameBox.Text).RapidTag;
        }

        void maxPlayersSlider_ValueChanged(object sender, EventArgs e)
        {
            maxPlayersLabel.Text = String.Format("Maximum Players ({0})", maxPlayersBar.Value);
            minPlayersBar.Maximum = maxPlayersBar.Value;
        }

        void minPlayersBar_ValueChanged(object sender, EventArgs e)
        {
            minPlayersLabel.Text = String.Format("MinimumPlayers ({0})", minPlayersBar.Value);
            maxPlayersBar.Minimum = minPlayersBar.Value;
        }

        void okButton_Click(object sender, EventArgs e)
        {
            Program.Conf.HasHosted = true;
            Program.Conf.HostBattle_MaxPlayers = maxPlayersBar.Value;
            Program.Conf.HostBattle_MinPlayers = minPlayersBar.Value;
            Program.Conf.HostBattle_Teams = teamsBar.Value;
            Program.Conf.HostBattle_Title = battleTitleBox.Text;
            Program.Conf.HostBattle_UseManage = enableManageBox.Checked;
            Program.Conf.HostBattle_SpringieCommands = springieCommandsBox.Text;
            Program.SaveConfig();

            DialogResult = DialogResult.OK;
        }

        void showAdvancedButton_Click(object sender, EventArgs e)
        {
            if (!advancedOptionsGroup.Visible) ShowAdvanced();
            else HideAdvanced();
        }

        void teamsBar_ValueChanged(object sender, EventArgs e)
        {
            teamsLabel.Text = String.Format("Teams ({0})", teamsBar.Value);
        }
    }
}