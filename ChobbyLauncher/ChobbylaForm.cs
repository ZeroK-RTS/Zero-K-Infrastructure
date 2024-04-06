using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlasmaDownloader;
using ZkData;
using System.Threading;
using PlasmaShared;

namespace ChobbyLauncher
{
    public partial class ChobbylaForm : Form
    {
        private Chobbyla chobbyla;
        private IChobbylaProgress progress => chobbyla.Progress;

        public ChobbylaForm(Chobbyla chobbyla)
        {
            this.chobbyla = chobbyla;
            InitializeComponent();
            btnCancel.Image = Shraka.exit.GetResized(16,16);
            btnCancel.ImageAlign = ContentAlignment.MiddleCenter;
            
            DoubleBuffered = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(255, 0, 30, 40));
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, DisplayRectangle, FrameBorderRenderer.StyleType.Shraka);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (
                MessageBox.Show(this,
                    "WARNING, cancelling now might corrupt your game copy.\nDo you really need to do it?",
                    "Confirm download cancel",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                progress.Download?.Abort();
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }


        private async void ChobbylaForm_Load(object sender, EventArgs e)
        {
            try
            {
                if (!await chobbyla.Prepare())
                {
                    MessageBox.Show(this, chobbyla.Status, "Failed to start", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.Cancel;
                }
                else DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error starting Chobby", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.Cancel;
            }
            Close();
        }


        private void ChobbylaForm_Closing(Object sender, FormClosingEventArgs e)
        {
            Thread.Sleep(500); // This allows Analystics to finish.
            Application.Exit(); // Lets close the process now.
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (lb1.Text != progress.Status) lb1.Text = progress.Status;
            var cd = progress.Download;
            if (cd != null)
            {
                lb1.Text = $"Downloading {cd.Name}  {cd.CurrentSpeed / 1024}kB/s  ETA: {cd.TimeRemaining}";
                progressBar1.Value = Math.Max(0, Math.Min(100, (int)Math.Round(cd.TotalProgress)));
            }
            if (progressBar1.Value == 0)
            {
                if (progressBar1.Style != ProgressBarStyle.Marquee) progressBar1.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                if (progressBar1.Style != ProgressBarStyle.Continuous) progressBar1.Style = ProgressBarStyle.Continuous;
            }
        }
    }
}