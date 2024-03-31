using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameAnalyticsSDK.Net;
using Octokit;
using PlasmaShared;
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
        private const int MaxInfologSize = 62000;
        private const string InfoLogLineStartPattern = @"(^\[t=\d+:\d+:\d+\.\d+\]\[f=-?\d+\] )";
        private const string InfoLogLineEndPattern = @"(\r?\n|\Z)";
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
        public static int FindFirstDesyncMessage(string logStr)
        {
            //[t=00:22:43.533864][f=0003461] Sync error for mankarse in frame 3451 (got 927a6f33, correct is 6b550dd1)
            try
            {
                //See ZkData.Account.IsValidLobbyName
                var accountNamePattern = @"[_[\]a-zA-Z0-9]{1,25}";
                var match =
                    Regex.Match(
                        logStr,
                        $@"Sync error for(?<={InfoLogLineStartPattern}Sync error for) {accountNamePattern} in frame \d+ \(got [a-z0-9]+, correct is [a-z0-9]+\){InfoLogLineEndPattern}",
                        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
                        TimeSpan.FromSeconds(30));

                return match.Success ? match.Index : -1;
            }
            catch(RegexMatchTimeoutException)
            {
                Trace.TraceError("[CrashReportHelper] RegexMatchTimeoutException in FindFirstDesyncMessage");
                return -1;
            }
        }

        private static string Truncate(string infolog, int maxSize)
        {
            var firstDesync = FindFirstDesyncMessage(infolog);
            var regionsOfInterest = new List<TextTruncator.RegionOfInterest>(firstDesync == -1 ? 2 : 3);

            regionsOfInterest.Add(new TextTruncator.RegionOfInterest { PointOfInterest = 0, StartLimit = 0, EndLimit = infolog.Length });
            if (firstDesync != -1)
            {
                regionsOfInterest.Add(new TextTruncator.RegionOfInterest { PointOfInterest = firstDesync, StartLimit = 0, EndLimit = infolog.Length });
            }
            regionsOfInterest.Add(new TextTruncator.RegionOfInterest { PointOfInterest = infolog.Length, StartLimit = 0, EndLimit = infolog.Length });

            return TextTruncator.Truncate(infolog, maxSize, regionsOfInterest);
        }

        public static void CheckAndReportErrors(string logStr, bool springRunOk, string bugReportTitle, string bugReportDescription, string engineVersion)
        {
            var syncError = FindFirstDesyncMessage(logStr) != -1;
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
