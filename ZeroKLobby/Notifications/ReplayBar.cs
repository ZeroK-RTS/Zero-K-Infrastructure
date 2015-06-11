using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using PlasmaDownloader;

namespace ZeroKLobby.Notifications
{
    public partial class ReplayBar : UserControl, INotifyBar
    {
        readonly string demoUrl;
        readonly string engineVersion;
        readonly string mapName;
        readonly string modName;


        public ReplayBar(string demoUrl, string modName, string mapName, string engineVersion)
        {
            this.demoUrl = demoUrl;
            this.modName = modName;
            this.mapName = mapName;
            this.engineVersion = engineVersion;
            InitializeComponent();
        }


      NotifyBarContainer container;
        public void AddedToContainer(NotifyBarContainer container) {
            this.container = container;
            container.Title = "Replay preparing";
            container.TitleTooltip = "Please await downloads";
        }

        public void CloseClicked(NotifyBarContainer container) {
            Program.NotifySection.RemoveBar(this);
        }

        public void DetailClicked(NotifyBarContainer container) {
        }

        public Control GetControl() {
            return this;
        }

        private void ReplayBar_Load(object sender, EventArgs e)
        {
            label1.Text = string.Format("Starting replay {0} - please wait", demoUrl);

            var waitHandles = new List<EventWaitHandle>();

            var downMod = Program.Downloader.GetResource(DownloadType.MOD, modName);
            if (downMod != null) waitHandles.Add(downMod.WaitHandle);

            var downMap = Program.Downloader.GetResource(DownloadType.MAP, mapName);
            if (downMap != null) waitHandles.Add(downMap.WaitHandle);

            var downDemo = Program.Downloader.GetResource(DownloadType.DEMO, demoUrl);
            if (downDemo != null) waitHandles.Add(downDemo.WaitHandle);

            var downEngine = Program.Downloader.GetAndSwitchEngine(engineVersion);
            if (downEngine != null) waitHandles.Add(downEngine.WaitHandle);

            ZkData.Utils.StartAsync(() =>
            {
                if (waitHandles.Any()) WaitHandle.WaitAll(waitHandles.ToArray());

                if ((downMod != null && downMod.IsComplete == false) || (downMap != null && downMap.IsComplete == false) ||
                    (downDemo != null && downDemo.IsComplete == false) || (downEngine != null && downEngine.IsComplete == false))
                {
                    Program.MainWindow.InvokeFunc(() =>
                    {
                        label1.Text = string.Format("Download of {0} failed", demoUrl);
                        container.btnStop.Enabled = true;
                    });
                    return;
                }

                var path = Utils.MakePath(Program.SpringPaths.WritableDirectory, "demos", new Uri(demoUrl).Segments.Last());
                try
                {
                    var optirun = Environment.GetEnvironmentVariable("OPTIRUN"); //get OPTIRUN filename from OS
                    string springFilename = Program.SpringPaths.Executable; //use springMT or standard
                    if (string.IsNullOrEmpty(optirun))
                    {
                        Process.Start(springFilename, string.Format("\"{0}\" --config \"{1}\"", path, Program.SpringPaths.GetSpringConfigPath()));
                    }
                    else
                    {
                        Process.Start(optirun, string.Format("\"{0}\" \"{1}\" --config \"{2}\"", springFilename, path, Program.SpringPaths.GetSpringConfigPath()));
                    }

                    Program.MainWindow.InvokeFunc(() => Program.NotifySection.RemoveBar(this));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error starting replay: {0}", ex);
                    Program.MainWindow.InvokeFunc(() =>
                    {
                        label1.Text = string.Format("Error starting replay {0}", demoUrl);
                        container.btnStop.Enabled = true;
                    });
                }
            });
        }
    }
}
