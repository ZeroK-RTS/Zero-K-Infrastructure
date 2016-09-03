using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PlasmaDownloader;
using ZeroKLobby.Controls;

namespace ZeroKLobby.MicroLobby
{
    public partial class WaitDownloadDialog : ZklBaseForm
    {
        private List<Download> downloads;
        public WaitDownloadDialog(IEnumerable<Download> downloads)
        {
            InitializeComponent();
            this.downloads = downloads.Where(x=>x!=null).ToList();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var page = Program.MainWindow.navigationControl.CurrentNavigatable as Control;
            if (page?.BackgroundImage != null) this.RenderControlBgImage(page, e);
            else e.Graphics.Clear(Config.BgColor);
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, DisplayRectangle, FrameBorderRenderer.StyleType.Shraka);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (downloads.All(x => x==null || x.IsComplete == true))
            {
                DialogResult =  DialogResult.OK;
                timer1.Stop();
                Close();
            }

            var down = downloads.FirstOrDefault(x => x!=null && x.IsComplete != true);
            if (down != null)
            {
                if (down.IsAborted || down.IsComplete == false)
                {
                    label1.Text = "Download of " + down.Name + " has failed";
                    btnRetry.Visible = true;
                }
                else
                {
                    label1.Text = "Downloading " + down.Name;
                    btnRetry.Visible = false;
                }

                progressBar1.Value = (int)Math.Round(down.TotalProgress);
                lbProgress.Text = $"{down.CurrentSpeed/1024}kB/s  ETA: {down.TimeRemaining}";
            }
        }

        private void bitmapButton1_Click(object sender, EventArgs e)
        {
            DialogResult=DialogResult.Cancel;
            timer1.Stop();
            Close();
        }

        private void bitmapButton2_Click(object sender, EventArgs e)
        {
            var down = downloads.FirstOrDefault(x => x!=null && x.IsComplete != true);
            if (down != null)
            {
                var newDown = Program.Downloader.GetResource(down.DownloadType, down.Name);
                if (newDown != null)
                {
                    downloads.Remove(down);
                    downloads.Add(newDown);
                }
                else downloads.Remove(down); // have it now
            }
        }
    }
}
