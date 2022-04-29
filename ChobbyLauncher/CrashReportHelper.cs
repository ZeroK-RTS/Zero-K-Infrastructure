using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameAnalyticsSDK.Net;
using Octokit;
using ZkData;
using FileMode = System.IO.FileMode;

namespace ChobbyLauncher
{
    public enum CrashType {
        Desync,
        Crash,
        LuaError,
        UserReport
    };

    public static class CrashReportHelper
    {
        private const string TruncatedString = "------- TRUNCATED -------";
        private const int MaxInfologSize = 250000;
        public static Issue ReportCrash(string infolog, CrashType type, string engine, string bugReportTitle, string bugReportDescription)
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("chobbyla"));
                client.Credentials = new Credentials(GlobalConst.CrashReportGithubToken);

                
                infolog = Truncate(infolog, MaxInfologSize);

                var createdIssue =
                    client.Issue.Create("ZeroK-RTS", "CrashReports", new NewIssue($"Spring {type} [{engine}] {bugReportTitle}") { Body = $"{bugReportDescription}\n\n```{infolog}```", })
                        .Result;

                return createdIssue;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Problem reporting a bug: {0}", ex);
            }
            return null;
        }

        public static bool IsDesyncMessage(string msg)
        {
            return !string.IsNullOrEmpty(msg) && msg.Contains(" Sync error for ") && msg.Contains(" in frame ") && msg.Contains(" correct is ");
        }


        private static string Truncate(string infolog, int maxSize)
        {
            if (infolog.Length > maxSize) // truncate infolog in middle
            {
                var lines = infolog.Lines();
                var firstPart = new List<string>();
                var lastPart = new List<string>();
                int desyncFirst = -1;

                for (int a = 0; a < lines.Length;a++)
                    if (IsDesyncMessage(lines[a]))
                    {
                        desyncFirst = a;
                        break;
                    }

                if (desyncFirst != -1)
                {
                    var sumSize = 0;
                    var firstIndex = desyncFirst;
                    var lastIndex = desyncFirst + 1;
                    do
                    {
                        if (firstIndex >= 0)
                        {
                            firstPart.Add(lines[firstIndex]);
                            sumSize += lines[firstIndex].Length;
                        }
                        if (lastIndex < lines.Length)
                        {
                            lastPart.Add(lines[lastIndex]);
                            sumSize += lines[lastIndex].Length;
                        }

                        firstIndex--;
                        lastIndex++;

                    } while (sumSize < MaxInfologSize && (firstIndex > 0 || lastIndex < lines.Length));
                    if (lastIndex < lines.Length) lastPart.Add(TruncatedString);
                    if (firstIndex > 0) firstPart.Add(TruncatedString);
                    firstPart.Reverse();
                }
                else
                {

                    var sumSize = 0;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        int index = i%2 == 0 ? i/2 : lines.Length - i/2 - 1;
                        if (sumSize + lines[index].Length < maxSize)
                        {
                            if (i%2 == 0) firstPart.Add(lines[index]);
                            else lastPart.Add(lines[index]);
                        }
                        else
                        {
                            firstPart.Add(TruncatedString);
                            break;
                        }
                        sumSize += lines[index].Length;
                    }
                    lastPart.Reverse();
                }

                infolog = string.Join("\r\n", firstPart) + "\r\n" + string.Join("\r\n", lastPart);
            }
            return infolog;
        }
        
        public static void CheckAndReportErrors(string logStr, bool springRunOk, string bugReportTitle, string bugReportDescription, string engineVersion)
        {
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
                    if (MessageBox.Show(
                        "You have outdated graphics card drivers!\r\nPlease try finding ones for your graphics card and updating them. \r\n\r\nWould you like to see our Linux graphics driver guide?",
                        "Outdated graphics card driver detected",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        Utils.OpenUrl("http://zero-k.info/mediawiki/index.php?title=Optimized_Graphics_Linux");
                    }
                }
                else
                {
                    MessageBox.Show("You have outdated graphics card drivers!\r\nPlease try finding ones for your graphics card and updating them.",
                        "Outdated graphics card driver detected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            var luaErr = logStr.Contains("LUA_ERRRUN");
            bool crashOccured = (!springRunOk && !openGlFail) || syncError || luaErr;
            bool isUserReport = !string.IsNullOrEmpty(bugReportTitle);

            if (crashOccured || isUserReport)
            {
                /* Don't make a popup for user reports since the user already agreed by clicking the report button earlier.
                 * NB: benchmarks via Chobby also work by creating a user report. */
                if (isUserReport || MessageBox.Show("We would like to send crash/desync data to Zero-K repository, it can contain chatlogs. Do you agree?",
                    "Automated crash report",
                    MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    var crashType = isUserReport ? CrashType.UserReport
                                  : syncError    ? CrashType.Desync
                                  : luaErr       ? CrashType.LuaError
                                                 : CrashType.Crash;

                    var ret = ReportCrash(logStr,
                        crashType,
                        engineVersion,
                        bugReportTitle,
                        bugReportDescription);
                    if (ret != null)
                        try
                        {
                            Utils.OpenUrl(ret.HtmlUrl);
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
