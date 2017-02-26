using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using GameAnalyticsSDK.Net;
using LumiSoft.Net.STUN.Client;
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
            if (!Debugger.IsAttached) Trace.Listeners.Add(new ConsoleTraceListener());

            var logStringBuilder = new StringBuilder();
            var threadSafeWriter = TextWriter.Synchronized(new StringWriter(logStringBuilder));
            Trace.Listeners.Add(new TextWriterTraceListener(threadSafeWriter));

            try
            {
                GameAnalytics.Initialize(GlobalConst.GameAnalyticsGameKey, GlobalConst.GameAnalyticsToken);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error starting GameAnalytics: {0}", ex);
            }

            string chobbyTag, engineOverride;
            ulong connectLobbyID;

            ParseCommandLine(args, out connectLobbyID, out chobbyTag, out engineOverride);

            var startupPath = Path.GetDirectoryName(Path.GetFullPath(Application.ExecutablePath));

            if (!SpringPaths.IsDirectoryWritable(startupPath))
            {
                MessageBox.Show("Please move this program to a writable folder",
                    "Cannot write to startup folder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                try
                {
                    GameAnalytics.AddErrorEvent(EGAErrorSeverity.Error, "Wrapper cannot start, folder not writable");
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error adding GA error event: {0}", ex);
                }
                Environment.Exit(0);
                return;
            }

            Application.EnableVisualStyles();

            try
            {
                var chobbyla = new Chobbyla(startupPath, chobbyTag, engineOverride);

                RunWrapper(chobbyla, connectLobbyID, threadSafeWriter, logStringBuilder);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error starting chobby: {0}", ex);
                MessageBox.Show(ex.ToString(), "Error starting Chobby", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                try
                {
                    GameAnalytics.AddErrorEvent(EGAErrorSeverity.Critical, "Wrapper crash: " + ex);
                }
                catch (Exception ex2)
                {
                    Trace.TraceError("Error adding GA error event: {0}", ex2);
                }
            }

            try
            {
                GameAnalytics.OnStop();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error ending GA session: {0}", ex);
            }

            Environment.Exit(0);
        }

        private static void ParseCommandLine(string[] args, out ulong connectLobbyID, out string chobbyTag, out string engineOverride)
        {
            connectLobbyID = 0;
            chobbyTag = null;
            engineOverride = null;
            if (args.Length > 0)
            {
                for (var i = 0; i < args.Length - 1; i++)
                {
                    var a = args[i];
                    if (a == "+connect_lobby")
                    {
                        ulong.TryParse(args[i + 1], out connectLobbyID);
                        args = args.Where((x, j) => (j != i) && (j != i + 1)).ToArray();

                        if (args.Length < 1) return;
                        break;
                    }
                }

                if ((args[0] == "--help") || (args[0] == "-h") || (args[0] == "/?"))
                {
                    Console.WriteLine(
                        "chobby.exe[rapid_tag][engine_override] \n\nUse zkmenu: stable or chobby: test\nTo run local dev version use chobby.exe dev");
                    MessageBox.Show(
                        "chobby.exe [rapid_tag] [engine_override] \n\nUse zkmenu:stable or chobby:test\nTo run local dev version use chobby.exe dev");
                }
                chobbyTag = args[0];
                if (args.Length > 1) engineOverride = args[1];
            }
        }

        private static void RunWrapper(Chobbyla chobbyla, ulong connectLobbyID, TextWriter logWriter, StringBuilder logSb)
        {
            if (!chobbyla.IsSteam) // not steam, show gui
            {
                var cf = new ChobbylaForm(chobbyla) { StartPosition = FormStartPosition.CenterScreen };
                if (cf.ShowDialog() != DialogResult.OK) return;
            }
            else if (!chobbyla.Prepare().Result) return; // otherwise just do simple prepare, no gui

            var springRunOk = chobbyla.Run(connectLobbyID, logWriter).Result;
            Trace.TraceInformation("Spring exited");
            logWriter.Flush();

            if (!springRunOk) // crash has occured
            {
                Trace.TraceWarning("Spring crash detected");
                if (
                    MessageBox.Show("We would like to send crash data to Zero-K repository, it can contain chat. Do you agree?",
                        "Automated crash report",
                        MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    var ret = CrashReportHelper.ReportCrash(logSb.ToString());
                    if (ret != null)
                        try
                        {
                            Process.Start(ret.HtmlUrl.ToString());
                        }
                        catch { }
                }

                try
                {
                    GameAnalytics.AddErrorEvent(EGAErrorSeverity.Critical, "Spring crash");
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error adding GA error event: {0}", ex);
                }
            }


        }
    }
}