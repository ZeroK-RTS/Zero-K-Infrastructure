using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using GameAnalyticsSDK.Net;
using LumiSoft.Net.STUN.Client;
using Steamworks;
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
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

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

            threadSafeWriter.Flush();
            try
            {
                File.WriteAllText(Path.Combine(startupPath, "infolog_full.txt"), logStringBuilder.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error writing full infolog: {0}",ex);
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
                        "Zero-K.exe [rapid_tag] [engine_override] \n\nUse zkmenu:stable or chobby:test\nTo run local dev version use Zero-K.exe dev");
                    MessageBox.Show(
                        "Zero-K.exe [rapid_tag] [engine_override] \n\nUse zkmenu:stable or chobby:test\nTo run local dev version use Zero-K.exe dev");
                }
                chobbyTag = args[0];
                if (args.Length > 1) engineOverride = args[1];
            }
        }

        private static void RunWrapper(Chobbyla chobbyla, ulong connectLobbyID, TextWriter logWriter, StringBuilder logSb)
        {
            if (!chobbyla.IsSteamFolder) // not steam, show gui
            {
                var cf = new ChobbylaForm(chobbyla) { StartPosition = FormStartPosition.CenterScreen };
                if (cf.ShowDialog() != DialogResult.OK) return;
            }
            else if (!chobbyla.Prepare().Result) return; // otherwise just do simple prepare, no gui

            var springRunOk = chobbyla.Run(connectLobbyID, logWriter);
            Trace.TraceInformation("Spring exited");
            if (!springRunOk) Trace.TraceWarning("Spring crash detected");
            
            logWriter.Flush();
            var logStr = logSb.ToString();

            var syncError = CrashReportHelper.IsDesyncMessage(logStr);
            if (syncError) Trace.TraceWarning("Sync error detected");

            var openGlFail = logStr.Contains("No OpenGL drivers installed.") ||
                logStr.Contains("This stack trace indicates a problem with your graphic card driver") ||
                logStr.Contains("Please go to your GPU vendor's website and download their drivers.") ||
                logStr.Contains("minimum required OpenGL version not supported, aborting") ||
                logStr.Contains("Update your graphic-card driver!");
            

            if (openGlFail)
            {
                Trace.TraceWarning("Outdated OpenGL detected");

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    if (MessageBox.Show("You have outdated graphics card drivers!\r\nPlease try finding ones for your graphics card and updating them. \r\n\r\nWould you like to see our Linux graphics driver guide?", "Outdated graphics card driver detected", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        Process.Start("http://zero-k.info/mediawiki/index.php?title=Optimized_Graphics_Linux");
                    }
                }
                else
                {
                    MessageBox.Show("You have outdated graphics card drivers!\r\nPlease try finding ones for your graphics card and updating them.", "Outdated graphics card driver detected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                    
            }

            var luaErr = logStr.Contains("LUA_ERRRUN");

            if ((!springRunOk && !openGlFail) || syncError || luaErr) // crash has occured
            {
                
                if (
                    MessageBox.Show("We would like to send crash/desync data to Zero-K repository, it can contain chat. Do you agree?",
                        "Automated crash report",
                        MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    var ret = CrashReportHelper.ReportCrash(logSb.ToString(), syncError ? CrashType.Desync : luaErr ? CrashType.LuaError : CrashType.Crash, chobbyla.engine);
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
