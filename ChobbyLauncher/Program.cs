using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LobbyClient;
using PlasmaDownloader;
using ZkData;

namespace ChobbyLauncher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var startupPath = Path.GetDirectoryName(Path.GetFullPath(Application.ExecutablePath));
            var paths = new SpringPaths(startupPath);
            var down = new PlasmaDownloader.PlasmaDownloader(new SpringScanner(paths), paths);
            List<Download> downloads = new List<Download>();
            downloads.Add(down.GetResource(DownloadType.MOD, "chobby:stable"));
            downloads.Add(down.GetResource(DownloadType.ENGINE, GlobalConst.DefaultEngineOverride));


            WaitHandle.WaitAll(downloads.Where(x => x != null).Select(x => x.WaitHandle).ToArray());

            var intName = down.PackageDownloader.GetByTag("chobby:stable");

            var spring = new Spring(paths);
            spring.LaunchChobby(intName.InternalName);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

        }
    }
}
