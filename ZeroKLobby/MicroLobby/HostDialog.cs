using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

namespace ZeroKLobby.MicroLobby
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
            if (Program.Conf.ShowOfficialBattles) gameBox.Items.Add(KnownGames.GetDefaultGame());
            else gameBox.Items.AddRange(KnownGames.List.ToArray());

            if (defaultGame == null || gameBox.Items.Cast<GameInfo>().SingleOrDefault(n => n == defaultGame) == null) gameBox.SelectedIndex = new Random().Next(0, gameBox.Items.Count);
            else gameBox.SelectedIndex = gameBox.Items.IndexOf(gameBox.Items.Cast<GameInfo>().Single(n => n == defaultGame));
            rapidTagBox.Text = KnownGames.List.Single(g => g.ToString() == gameBox.Text).RapidTag;
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

        int DPIScaleUp(int designHeight)
        {
            //-- code for scaling-up based on user's custom DPI.
            Graphics formGraphics = this.CreateGraphics(); //Reference: http://msdn.microsoft.com/en-us/library/system.drawing.graphics.dpix.aspx .ie: NotifyBarContainer.cs
            float formDPIvertical = formGraphics.DpiY; //get current DPI
            float scaleUpRatio = formDPIvertical / 96; //get scaleUP ratio, 96 is the original DPI
            //--
            return ((int)(designHeight * scaleUpRatio)); //multiply the scaleUP ratio to the original design height, then change type to integer, then return value;
        }

        void HideAdvanced()
        {
            int designHeight = 480;
            Height = DPIScaleUp(designHeight);
            advancedOptionsGroup.Visible = false;
            showAdvancedButton.Text = "Show Advanced Options";
        }

        void ShowAdvanced()
        {
            int designHeight = 650;
            Height = DPIScaleUp(designHeight);
            advancedOptionsGroup.Visible = true;
            showAdvancedButton.Text = "Hide Advanced Options";
        }

        void HostDialog_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = Resources.star;
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
            rapidTagBox.Text = KnownGames.List.Single(g => g.ToString() == gameBox.Text).RapidTag;
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