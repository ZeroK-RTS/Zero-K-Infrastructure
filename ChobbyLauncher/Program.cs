using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Octokit;
using ZkData;
using Application = System.Windows.Forms.Application;

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
            string engineOverride = null;
            ulong connectLobbyID = 0;

            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length-1; i++)
                {
                    var a = args[i];
                    if (a == "+connect_lobby")
                    {
                        ulong.TryParse(args[i+1], out connectLobbyID);
                        args = args.Where((x, j) => j != i && j != i + 1).ToArray();
                        break;
                    }
                }


                if (args[0] == "--help" || args[0] == "-h" || args[0] == "/?")
                {
                    MessageBox.Show("chobby.exe [rapid_tag] [engine_override] \n\nUse zkmenu:stable or chobby:test\nTo run local dev version use chobby.exe dev");
                }
                chobbyTag = args[0];
                if (args.Length > 1) engineOverride = args[1];
            }

            var startupPath = Path.GetDirectoryName(Path.GetFullPath(Application.ExecutablePath));
            if (!SpringPaths.IsDirectoryWritable(startupPath))
            {
                MessageBox.Show("Please move this program to a writable folder", "Cannot write to startup folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.EnableVisualStyles();

            try
            {
                var chobbyla = new Chobbyla(startupPath, chobbyTag, engineOverride, connectLobbyID);
                var cf = new ChobbylaForm(chobbyla) { StartPosition = FormStartPosition.CenterScreen };
                if (cf.ShowDialog() == DialogResult.OK)
                {
                    if (!chobbyla.Run().Result && MessageBox.Show("We would like to send crash data to Zero-K repository, it can contain chat. Do you agree?",
                        "Automated crash report", MessageBoxButtons.OKCancel) == DialogResult.OK)
                    {
                        var ret = CrashReportHelper.ReportCrash(chobbyla.paths);
                        if (ret != null)
                        {
                            try
                            {
                                Process.Start(ret.Url.ToString());
                            } catch { }
                        }
                    }
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error starting chobby: {0}", ex);
                MessageBox.Show(ex.ToString(), "Error starting Chobby", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }
    }
}