using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby.Controls;

namespace ZeroKLobby.MicroLobby
{
    public partial class HostDialog: ZklBaseForm
    {
        public string BattleTitle { get { return battleTitleBox.Text; } }

        public string GameName { get { return game.FullName; } }

        public string GameRapidTag { get { return game.RapidTag; } }

        public string Password { get { return passwordBox.Text; } }

        private GameInfo game;

        public HostDialog(GameInfo defaultGame)
        {
            InitializeComponent();
            battleTitleBox.Text = Program.TasClient.MyUser + "'s Battle";

            game = defaultGame ?? KnownGames.List.First(x => x.IsPrimary);
            if (Program.Conf.HasHosted)
            {
                try
                {
                    battleTitleBox.Text = Program.Conf.HostBattle_Title;
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Error in restoring battle configuration: " + e);
                }
            }
        }



        void okButton_Click(object sender, EventArgs e)
        {
            Program.Conf.HasHosted = true;
            Program.Conf.HostBattle_Title = battleTitleBox.Text;
            //Program.Conf.HostBattle_SpringieCommands = springieCommandsBox.Text;

            Program.SaveConfig();

            DialogResult = DialogResult.OK;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var page = Program.MainWindow.navigationControl.CurrentNavigatable as Control;
            if (page?.BackgroundImage != null) this.RenderControlBgImage(page, e);
            else e.Graphics.Clear(Config.BgColor);
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, DisplayRectangle, FrameBorderRenderer.StyleType.Shraka);
        }


    }
}