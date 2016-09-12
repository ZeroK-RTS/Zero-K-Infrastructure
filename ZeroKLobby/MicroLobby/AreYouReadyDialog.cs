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
        private TasClient client;
        
        public AreYouReadyDialog(TasClient client)
        {
            this.client = client;

            InitializeComponent();

            client.AreYouReadyStarted += (s, rdy) =>
            {
                areYou = rdy;
                lb1.Text = "Match found, are you ready?";
                cancelButton.Visible = true;
                okButton.Visible = true;
                lbTimer.Visible = true;
                created = DateTime.UtcNow;
                timer1.Start();
                StartPosition = FormStartPosition.CenterScreen;
                Program.MainWindow.NotifyUser("", "Match found", true, true);
                Show(Program.MainWindow);
            };


            client.AreYouReadyUpdated += (s, rdu) =>
            {
                var label = rdu.ReadyAccepted ? "You are ready, waiting for more people\n" : "Match found, are you ready?\n";
                label += $"Ready: {String.Join(", ", rdu.QueueReadyCounts.Select(x => $"{x.Key} ({x.Value})"))}\n";
                label += $"Refused: {String.Join(", ", rdu.QueueReadyCounts.Select(x => $"{x.Key} ({x.Value})"))}\n";
                if (rdu.LikelyToPlay) label += "Hurray, should start soon!";
                lb1.Text = label;

                if (rdu.ReadyAccepted)
                {
                    cancelButton.Visible = false;
                    okButton.Visible = false;
                }
            };

            client.AreYouReadyClosed += (s, result) =>
            {
                if (result.IsBattleStarting)
                {
                    Close();
                }
                else
                {
                    var txt = "Match failed, not enough people ready";
                    txt += result.AreYouBanned ? "\nYou cannot use MatchMaker for some time, because you werent ready" : "";
                    lb1.Text = txt;
                    areYou.SecondsRemaining = 0;
                    okButton.Visible = true;
                    cancelButton.Visible = false;
                }
            };
        }



        void okButton_Click(object sender, EventArgs e)
        {
            Program.TasClient.AreYouReadyResponse(true);
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
            Program.TasClient.AreYouReadyResponse(false);
            Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var secsLeft = (areYou.SecondsRemaining - (int)DateTime.UtcNow.Subtract(created).TotalSeconds);
            
            if (areYou.SecondsRemaining > 0) lbTimer.Text = string.Format("{0}s", secsLeft);
            if (areYou.SecondsRemaining > 0 && secsLeft <= 0)
            {
                lbTimer.Visible = false;
                okButton.Visible = true;
                timer1.Stop();
            }
        }

        private void AreYouReadyDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Stop();
        }
    }
}