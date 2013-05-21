using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
    public partial class HostDialog: Form
    {
        public string BattleTitle { get { return battleTitleBox.Text; } }

        public string GameName { get { return rapidTagBox.Text; } }

        public string Password { get { return passwordBox.Text; } }
        public string SpringieCommands { get { return springieCommandsBox.Text; } }

        public HostDialog(GameInfo defaultGame)
        {
            InitializeComponent();
            gameBox.SelectedIndexChanged += gameBox_TextChanged;
            battleTitleBox.Text = Program.TasClient.MyUser + "'s Battle";
            HideAdvanced();
            gameBox.DropDownStyle = ComboBoxStyle.DropDownList;
            if (Program.Conf.ShowOfficialBattles) gameBox.Items.AddRange(KnownGames.List.Where(x=>x.IsPrimary).ToArray());
            else gameBox.Items.AddRange(KnownGames.List.ToArray());

            if (defaultGame == null || gameBox.Items.Cast<GameInfo>().SingleOrDefault(n => n == defaultGame) == null) gameBox.SelectedIndex = new Random().Next(0, gameBox.Items.Count);
            else gameBox.SelectedIndex = gameBox.Items.IndexOf(gameBox.Items.Cast<GameInfo>().Single(n => n == defaultGame));
            rapidTagBox.Text = KnownGames.List.Single(g => g.ToString() == gameBox.Text).RapidTag;
            if (Program.Conf.HasHosted)
            {
                try
                {
                    battleTitleBox.Text = Program.Conf.HostBattle_Title;
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
            int designHeight = 235;
            DpiMeasurement.DpiXYMeasurement(this); //this measurement use cached value. It won't cost anything if another measurement was already done in other control element
            Height = DpiMeasurement.ScaleValueY(designHeight); //DpiMeasurement is a static class stored in ZeroKLobby\Util.cs
            advancedOptionsGroup.Visible = false;
            showAdvancedButton.Text = "Show Advanced Options";
        }

        void ShowAdvanced()
        {
            int designHeight = 405;
            DpiMeasurement.DpiXYMeasurement(this);
            Height = DpiMeasurement.ScaleValueY(designHeight);
            advancedOptionsGroup.Visible = true;
            showAdvancedButton.Text = "Hide Advanced Options";
        }

        void HostDialog_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = ZklResources.star;
        }


        void gameBox_TextChanged(object sender, EventArgs e)
        {
            rapidTagBox.Text = KnownGames.List.Single(g => g.ToString() == gameBox.Text).RapidTag;
        }


        void okButton_Click(object sender, EventArgs e)
        {
            Program.Conf.HasHosted = true;
            Program.Conf.HostBattle_Title = battleTitleBox.Text;
            Program.Conf.HostBattle_SpringieCommands = springieCommandsBox.Text;

            Program.SaveConfig();

            DialogResult = DialogResult.OK;
        }

        void showAdvancedButton_Click(object sender, EventArgs e)
        {
            if (!advancedOptionsGroup.Visible) ShowAdvanced();
            else HideAdvanced();
        }

    }
}