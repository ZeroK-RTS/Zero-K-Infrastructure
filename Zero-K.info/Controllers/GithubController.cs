using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using LobbyClient;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json.Linq;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class GithubController : Controller
    {
        [HttpPost]
        public async Task<ActionResult> Hook()
        {
            var eventType = Request.Headers["X-Github-Event"];
            var signature = Request.Headers["X-Hub-Signature"].Substring(5);


            var ms = new MemoryStream();
            Request.InputStream.CopyTo(ms);
            byte[] data = ms.ToArray();

            var secretKey = new Secrets().GetGithubHookKey();
            var hash = new HMACSHA1(Encoding.ASCII.GetBytes(secretKey)).ComputeHash(data);

            if (!string.Equals(hash.ToHex(), signature, StringComparison.InvariantCultureIgnoreCase)) return Content("Signature does not match");
            
            dynamic payload = JObject.Parse(Encoding.UTF8.GetString(data));

            string channel = "zkdev";
            if (eventType == "issues" && payload.repository.name == "CrashReports") channel = "crashreports";

            string text = FormatEvent(eventType, payload, false);
            if (text != null) await Global.Server.GhostChanSayMarkdown(channel, text, FormatEvent(eventType, payload, true));

            return Content("");
        }

        // the rest (edited, labeled, synchronize, ...) is noise
        private static readonly HashSet<string> relayedActions = new HashSet<string>() { "opened", "reopened", "closed" };

        /// <summary>
        ///     One line per event or commit, without PR bodies or full commit messages.
        ///     Markdown is for discord, where links collapse to their label; chobby needs bare urls at the end of a line to make them clickable
        /// </summary>
        private static string FormatEvent(string eventType, dynamic payload, bool markdown)
        {
            string repo = payload.repository.name;
            string sender = payload.sender.login;
            string action = payload.action;

            switch (eventType)
            {
                case "issues":
                    if (!relayedActions.Contains(action)) return null;
                    return FormatLine($"{sender} {action} issue ", $"{repo}#{payload.issue.number}", (string)payload.issue.html_url, $": {payload.issue.title}", markdown);

                case "pull_request":
                    if (!relayedActions.Contains(action)) return null;
                    dynamic pr = payload.pull_request;
                    if (action == "closed" && (bool)pr.merged) action = "merged";
                    return FormatLine($"{sender} {action} PR ", $"{repo}#{payload.number}", (string)pr.html_url, $": {pr.title} (+{pr.additions} -{pr.deletions})", markdown);

                case "push":
                    string branch = payload["ref"];
                    if (branch != "refs/heads/master" && branch != "refs/heads/stable") return null;

                    var commits = (JArray)payload.commits;
                    if (commits.Count == 0) return null;

                    var sb = new StringBuilder(FormatLine($"{sender} pushed ", $"{commits.Count} commit{(commits.Count == 1 ? "" : "s")}", (string)payload.compare, $" to {repo}/{branch.Substring("refs/heads/".Length)}:", markdown));
                    foreach (dynamic commit in commits)
                    {
                        string id = commit.id;
                        string message = commit.message;
                        sb.Append('\n').Append(FormatLine("", id.Substring(0, 7), (string)commit.url, " " + message.Split('\n')[0].TrimEnd(), markdown));
                    }
                    return sb.ToString();
            }

            return null;
        }

        private static string FormatLine(string before, string label, string url, string after, bool markdown)
        {
            return markdown ? $"{before}[{label}](<{url}>){after}" : $"{before}{label}{after} {url}";
        }
    }
}
