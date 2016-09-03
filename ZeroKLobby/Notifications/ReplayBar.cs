using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using PlasmaDownloader;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Notifications
{
    public partial class ReplayBar: ZklNotifyBar
    {
        private readonly string demoUrl;
        private readonly string engineVersion;
        private readonly string mapName;
        private readonly string modName;


        public ReplayBar(string demoUrl, string modName, string mapName, string engineVersion)
        {
            this.demoUrl = demoUrl;
            this.modName = modName;
            this.mapName = mapName;
            this.engineVersion = engineVersion;
            InitializeComponent();
        }


        private void ReplayBar_Load(object sender, EventArgs e)
        {
            label1.Text = string.Format("Starting replay {0} - please wait", demoUrl);

            var downloads = new List<Download>();
            downloads.Add(Program.Downloader.GetResource(DownloadType.ENGINE, engineVersion));
            downloads.Add(Program.Downloader.GetResource(DownloadType.MOD, modName));
            downloads.Add(Program.Downloader.GetResource(DownloadType.MAP, mapName));
            downloads.Add(Program.Downloader.GetResource(DownloadType.DEMO, demoUrl));
            downloads = downloads.Where(x => x != null).ToList();
            if (downloads.Count > 0)
            {
                var dd = new WaitDownloadDialog(downloads);
                if (dd.ShowDialog(Program.MainWindow) == DialogResult.Cancel)
                {
                    Program.NotifySection.RemoveBar(this);
                    return;
                }
            }

            var path = Utils.MakePath(Program.SpringPaths.WritableDirectory, "demos", new Uri(demoUrl).Segments.Last());
            try
            {
                var optirun = Environment.GetEnvironmentVariable("OPTIRUN"); //get OPTIRUN filename from OS
                var springFilename = Program.SpringPaths.GetSpringExecutablePath(engineVersion); //use springMT or standard
                if (string.IsNullOrEmpty(optirun))
                {
                    Process.Start(springFilename, string.Format("\"{0}\" --config \"{1}\"", path, Program.SpringPaths.GetSpringConfigPath()));
                }
                else
                {
                    Process.Start(optirun,
                        string.Format("\"{0}\" \"{1}\" --config \"{2}\"", springFilename, path, Program.SpringPaths.GetSpringConfigPath()));
                }

                Program.MainWindow.InvokeFunc(() => Program.NotifySection.RemoveBar(this));
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error starting replay: {0}", ex);
                Program.MainWindow.InvokeFunc(() =>
                {
                    label1.Text = string.Format("Error starting replay {0}", demoUrl);
                    //container.btnStop.Enabled = true;
                });
            }
        }
    }
}