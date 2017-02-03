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

                if (MessageBox.Show("We would like to send crash data to Zero-K repository, it can contain chat. Do you agree?",
                        "Automated crash report",
                        MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    var createdIssue = client.Issue.Create("ZeroK-RTS",
                            "CrashReports",
                            new NewIssue("Spring crash")
                            {
                                Body = $"```{File.ReadAllText(Path.Combine(paths.WritableDirectory, "infolog.txt"))}```",
                            }).Result;

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
