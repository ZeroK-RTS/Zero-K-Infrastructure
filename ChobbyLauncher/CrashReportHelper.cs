using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Octokit;
using ZkData;
using FileMode = System.IO.FileMode;

namespace ChobbyLauncher
{
    public static class CrashReportHelper
    {
        public static Issue ReportCrash(SpringPaths paths)
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("chobbyla"));
                client.Credentials = new Credentials(GlobalConst.CrashReportGithubToken);
                using (
                    var fs = File.Open(Path.Combine(paths.WritableDirectory, "infolog.txt"),
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete))
                using (var streamReader = new StreamReader(fs))
                {
                    var createdIssue =
                        client.Issue.Create("ZeroK-RTS", "CrashReports", new NewIssue("Spring crash") { Body = $"```{streamReader.ReadToEnd()}```", })
                            .Result;

                    return createdIssue;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Problem reporting a bug: {0}", ex);
            }
            return null;
        }


    }
}
