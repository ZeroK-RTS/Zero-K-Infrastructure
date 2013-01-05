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
            int designHeight = 235;
            Height = DPIScaleUp(designHeight);
            advancedOptionsGroup.Visible = false;
            showAdvancedButton.Text = "Show Advanced Options";
        }

        void ShowAdvanced()
        {
            int designHeight = 405;
            Height = DPIScaleUp(designHeight);
            advancedOptionsGroup.Visible = true;
            showAdvancedButton.Text = "Hide Advanced Options";
        }

        void HostDialog_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = Resources.star;
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