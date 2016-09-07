using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ZkData;

namespace ChobbyLauncher
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            string chobbyTag = null;
            if (args.Length > 0)
            {
                if (args[0] == "--help" || args[0] == "-h" || args[0] == "/?")
                {
                    MessageBox.Show("chobby.exe [rapid_tag] \n\nUse chobby:stable or chobby:test");
                }

                chobbyTag = args[0];
            }

            var startupPath = Path.GetDirectoryName(Path.GetFullPath(Application.ExecutablePath));
            if (!SpringPaths.IsDirectoryWritable(startupPath))
            {
                MessageBox.Show("Please move this program to a writable folder");
                return;
            }

            var paths = new SpringPaths(startupPath, false);

            Application.EnableVisualStyles();

            var cf = new ChobbylaForm(chobbyTag, paths) { StartPosition = FormStartPosition.CenterScreen };
            cf.ShowDialog();
        }
    }
}