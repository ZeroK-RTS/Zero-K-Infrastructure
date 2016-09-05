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
        
        static void Main()
        {
            var startupPath = Path.GetDirectoryName(Path.GetFullPath(Application.ExecutablePath));
            var eng = "103.0.1-95-g20ebb8c";
            var paths = new SpringPaths(startupPath, false);
            var down = new PlasmaDownloader.PlasmaDownloader(new SpringScanner(paths), paths);
            List<Download> downloads = new List<Download>();
            downloads.Add(down.GetResource(DownloadType.MOD, "chobby:test"));
            downloads.Add(down.GetResource(DownloadType.ENGINE, eng));

            var handles = downloads.Where(x => x != null).Select(x => x.WaitHandle).ToArray();
            if (handles.Length > 0) WaitHandle.WaitAll(handles);

            var intName = down.PackageDownloader.GetByTag("chobby:test");

            var spring = new Spring(paths);
            spring.LaunchChobby(intName.InternalName, eng);


        //    Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

        }
    }
}
