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
        private string chobbyTag;
        private Download currentDownload;
        private SpringPaths paths;

        public ChobbylaForm(string chobbyTag, SpringPaths paths)
        {
            InitializeComponent();
            this.chobbyTag = chobbyTag ?? "chobby:stable";
            this.paths = paths;
        }


        private async void ChobbylaForm_Load(object sender, EventArgs e)
        {
            lb1.Text = "Checking for self-upgrade";

            var task = new Task<bool>(() => new SelfUpdater().CheckForUpdate());
            task.Start();
            await task;

            lb1.Text = "Checking for chobby update";



            var downloader = new PlasmaDownloader.PlasmaDownloader(new SpringScanner(paths) { WatchingEnabled = false, UseUnitSync = false }, paths);
            currentDownload = downloader.GetResource(DownloadType.MOD, chobbyTag);

            var asTask = currentDownload?.WaitHandle.AsTask(TimeSpan.FromMinutes(20));
            if (asTask != null) await asTask;
            if (currentDownload?.IsComplete == false)
            {
                MessageBox.Show(this, $"Download of {currentDownload.Name} has failed");
                DialogResult = DialogResult.Cancel;
                Close();
            }

            var ver = downloader.PackageDownloader.GetByTag(chobbyTag);
            if (ver != null)
            {
                var mi = ver.ReadFile(paths, "modinfo.lua");
                var lua = new Lua();
                var luaEnv = lua.CreateEnvironment();
                dynamic result = luaEnv.DoChunk(new StreamReader(mi), "dummy.lua");
                var engineVersion = result.engine ?? "103.0.1-95-g20ebb8c";

                lb1.Text = "Downloading engine";
                currentDownload = downloader.GetResource(DownloadType.ENGINE, engineVersion);
                var engDlTask = currentDownload?.WaitHandle.AsTask(TimeSpan.FromMinutes(20));
                if (engDlTask != null) await engDlTask;
                if (currentDownload?.IsComplete == false)
                {
                    MessageBox.Show(this, $"Download of engine {currentDownload.Name} has failed");
                    DialogResult = DialogResult.Cancel;
                    Close();
                }

                lb1.Text = "Starting";
                var chobbyla = new Chobbyla();
                chobbyla.LaunchChobby(paths, ver.InternalName, engineVersion);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(this, "Unexpected failure at reading the chobby rapid package");
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            var cd = currentDownload;
            if (cd != null)
            {
                lb1.Text = $"Downloading {cd.Name}  {cd.CurrentSpeed/1024}kB/s  ETA: {cd.TimeRemaining}";
                progressBar1.Value = (int)Math.Round(cd.TotalProgress);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(255, 0, 30, 40));
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, DisplayRectangle, FrameBorderRenderer.StyleType.Shraka);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            currentDownload?.Abort();
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
