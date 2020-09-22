﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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

            string text = null;
            string channel = "zkdev";
            Object[] values;

            switch (eventType) {
                case "issues":
                    if (payload.action == "labeled"){
                        break;
                    }
                    if (payload.repository.name == "CrashReports"){
                        channel = "crashreports";
                    }
                    values = new [] {payload.repository.name ,payload.sender.login,  payload.action, payload.issue.title, payload.issue.html_url};
                    text = string.Format("[{0}] {1} has {2} issue {3} <{4}>",values);
                    break;

                case "pull_request":
                    values = new [] {payload.repository.name ,payload.sender.login,  payload.action, payload.number, payload.pull_request.title , payload.pull_request.html_url, payload.pull_request.body};
                    text = string.Format("[{0}] {1} has {2} pull request #{3}: {4} <{5}>\n{6}",values);
                    break;

                case "push":
                    if (payload["ref"] != "refs/heads/master"
                    &&  payload["ref"] != "refs/heads/stable")
                        break;

                    var sb = new StringBuilder();
                    int count = 0;
                    dynamic commits = payload.commits;
                    foreach (dynamic commit in commits)
                    {
                        sb.AppendFormat("\n {0} <{1}>", commit.message, commit.url);
                        count++;
                    }
                    if (count > 0) text = $"[{payload.repository.name}] {payload.sender.login} has pushed {count} commits: {sb}";
                    break;
            }

            if (text != null) await Global.Server.GhostChanSay(channel, text);

            return Content("");
        }
    }
}
