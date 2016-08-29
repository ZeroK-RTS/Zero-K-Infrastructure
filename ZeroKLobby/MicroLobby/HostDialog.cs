using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using PlasmaShared;
using ZeroKLobby.Controls;
using ZkData;

namespace ZeroKLobby.MicroLobby
{
    public partial class HostDialog : ZklBaseForm
    {
        public string BattleTitle { get { return battleTitleBox.Text; } }

        public string Password { get { return passwordBox.Text; } }

        public AutohostMode? Mode => Enum.GetValues(typeof(AutohostMode)).Cast<AutohostMode>().FirstOrDefault(x => x.Description() == cbType.SelectedItem.ToString());
        
        public HostDialog(GameInfo defaultGame)
        {
            InitializeComponent();
            ZklBaseControl.Init(cbType);


            cbType.Items.Add(AutohostMode.GameChickens.Description());
            cbType.Items.Add(AutohostMode.Teams.Description());
            cbType.Items.Add(AutohostMode.Game1v1.Description());
            cbType.Items.Add(AutohostMode.GameFFA.Description());
            cbType.Items.Add(AutohostMode.None.Description());

            cbType.SelectedIndex = 0;

            battleTitleBox.Text = Program.TasClient.UserName + "'s game";

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

        private void cbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            battleTitleBox.Text = Program.TasClient.UserName + "'s " + cbType.SelectedItem;
        }
    }
}