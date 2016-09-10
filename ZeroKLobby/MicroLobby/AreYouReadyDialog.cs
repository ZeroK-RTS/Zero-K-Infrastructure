using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using ZeroKLobby.Controls;
using ZkData;

namespace ZeroKLobby.MicroLobby
{
    public partial class AreYouReadyDialog : ZklBaseForm
    {
        private AreYouReady areYou;
        private DateTime created;
        
        public AreYouReadyDialog(AreYouReady areYou)
        {
            this.areYou = areYou;
            InitializeComponent();
            lb1.Text = areYou.Text;
            if (!areYou.NeedReadyResponse)
            {
                cancelButton.Visible = false;
                lbTimer.Visible = false;
            }
            created = DateTime.UtcNow;
            timer1.Start();
        }



        void okButton_Click(object sender, EventArgs e)
        {
            if (areYou.NeedReadyResponse) Program.TasClient.AreYouReadyResponse(true);
            Close();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var page = Program.MainWindow.navigationControl.CurrentNavigatable as Control;
            if (page?.BackgroundImage != null) this.RenderControlBgImage(page, e);
            else e.Graphics.Clear(Config.BgColor);
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, DisplayRectangle, FrameBorderRenderer.StyleType.Shraka);
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            if (areYou.NeedReadyResponse) Program.TasClient.AreYouReadyResponse(false);
            Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var secsLeft = (areYou.SecondsRemaining - (int)DateTime.UtcNow.Subtract(created).TotalSeconds);
            
            if (areYou.SecondsRemaining > 0) lbTimer.Text = string.Format("{0}s", secsLeft);
            if (areYou.SecondsRemaining > 0 && secsLeft <= 0) Close();
        }

        private void AreYouReadyDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Stop();
        }
    }
}