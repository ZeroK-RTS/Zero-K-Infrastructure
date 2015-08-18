using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using LobbyClient;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json.Linq;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class GithubController : Controller
    {
        [HttpPost]
        public ActionResult Hook()
        {
            var eventType = Request.Headers["X-Github-Event"];
            var signature = Request.Headers["X-Hub-Signature"].Substring(5);


            var ms = new MemoryStream();
            Request.InputStream.CopyTo(ms);
            byte[] data = ms.ToArray();

            var secretKey = new Secrets().GetGithubHookKey(new ZkDataContext());
            var hash = new HMACSHA1(Encoding.ASCII.GetBytes(secretKey)).ComputeHash(data);

            if (!string.Equals(hash.ToHex(), signature, StringComparison.InvariantCultureIgnoreCase)) return Content("Signature does not match");
            
            dynamic payload = JObject.Parse(Encoding.UTF8.GetString(data));

            string text = null;
            
            Object[] values;

            switch (eventType) {
                case "issues":
                    values = new [] {payload.repository.name ,payload.sender.login,  payload.action, payload.issue.title, payload.issue.html_url};
                    text = string.Format("[{0}] {1} has {2} issue {3} {4}",values);
                    break;

                case "pull_request":
                    values = new [] {payload.repository.name ,payload.sender.login,  payload.action, payload.number, payload.pull_request.title , payload.pull_request.html_url, payload.pull_request.body};
                    text = string.Format("[{0}] {1} has {2} pull request #{3}: {4} ({5})\n{6}",values);
                    break;

                case "push":
                    List<string> commitMessages = new List<string>();
                    foreach (dynamic commit in payload.commits)
                    {
                        commitMessages.Add(commit.message);
                    }

                    text = $"[{payload.repository.name}] {payload.sender.login} has pushed {commitMessages.Count} commits: {payload.compare}\n{string.Join("\n", commitMessages)}";
                    break;
            }

            if (text != null) Global.Server.GhostChanSay("zkdev", text);

            return Content("");
        }
    }
}
