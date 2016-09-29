using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using PlasmaDownloader;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.Notifications;

namespace ZeroKLobby
{
    public class ReplayStarter{
        
        public void StartReplay(string demoUrl, string modName, string mapName, string engineVersion)
        {
            var downloads = new List<Download>();
            downloads.Add(Program.Downloader.GetResource(DownloadType.ENGINE, engineVersion));
            downloads.Add(Program.Downloader.GetResource(DownloadType.RAPID, modName));
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

                Program.SpringPaths.SetDefaultEnvVars(null, engineVersion);
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
                    WarningBar.DisplayWarning(string.Format("Error starting replay {0}", demoUrl));
                });
            }
        }
    }
}