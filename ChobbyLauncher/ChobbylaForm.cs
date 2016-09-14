using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Neo.IronLua;
using PlasmaDownloader;
using ZkData;

namespace ChobbyLauncher
{
    public partial class ChobbylaForm : Form
    {
        private Chobbyla chobbyla;

        public ChobbylaForm(Chobbyla chobbyla)
        {
            InitializeComponent();
            DoubleBuffered = true;
            this.chobbyla = chobbyla;
        }


        private async void ChobbylaForm_Load(object sender, EventArgs e)
        {
            if (!await chobbyla.Run()) MessageBox.Show(this, chobbyla.Status, "Failed to start", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            if (lb1.Text != chobbyla.Status) lb1.Text = chobbyla.Status;
            var cd = chobbyla.Download;
            if (cd != null)
            {
                lb1.Text = $"Downloading {cd.Name}  {cd.CurrentSpeed/1024}kB/s  ETA: {cd.TimeRemaining}";
                progressBar1.Value = (int)Math.Round(cd.TotalProgress);
                
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

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(255, 0, 30, 40));
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, DisplayRectangle, FrameBorderRenderer.StyleType.Shraka);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            chobbyla.Download?.Abort();
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
