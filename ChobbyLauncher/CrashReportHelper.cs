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
        private const int MaxInfologSize = 250000;
        public static Issue ReportCrash(string infolog, bool isDesync, string engine)
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("chobbyla"));
                client.Credentials = new Credentials(GlobalConst.CrashReportGithubToken);

                
                infolog = Truncate(infolog, MaxInfologSize);

                var createdIssue =
                    client.Issue.Create("ZeroK-RTS", "CrashReports", new NewIssue($"Spring {(isDesync ? "desync" : "crash")} [{engine}]") { Body = $"```{infolog}```", })
                        .Result;

                return createdIssue;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Problem reporting a bug: {0}", ex);
            }
            return null;
        }

        private static string Truncate(string infolog, int maxSize)
        {
            if (infolog.Length > maxSize) // truncate infolog in middle
            {
                var lines = infolog.Lines();
                var firstPart = new List<string>();
                var lastPart = new List<string>();
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
                        firstPart.Add("------- TRUNCATED -------");
                        break;
                    }
                    sumSize += lines[index].Length;
                }
                lastPart.Reverse();

                infolog = string.Join("\r\n", firstPart) + "\r\n" + string.Join("\r\n", lastPart);
            }
            return infolog;
        }
    }
}
