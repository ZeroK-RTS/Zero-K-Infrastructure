using System;
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
            var signature = Request.Headers["X-Hub-Signature"];

            var secretKey = new Secrets().GetGithubHookKey(new ZkDataContext());
            var hash = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey)).ComputeHash(Request.InputStream);
            if (!string.Equals(hash.ToHex(), signature, StringComparison.InvariantCultureIgnoreCase)) return Content("Signature does not match");
            Request.InputStream.Seek(0, SeekOrigin.Begin);

            var content = new StreamReader(Request.InputStream).ReadToEnd();

            dynamic payload = JObject.Parse(content);

            string text = null;

            switch (eventType) {
                case "issues":
                    text = string.Format("{0} has {1} issue {2} {3}",payload.sender.login,  payload.action, payload.issue.title, payload.issue.url);
                    break;
            }

            if (text != null) Global.Nightwatch.Tas.Say(SayPlace.Channel, "zkdev", text, true);

            return Content("");
        }
    }
}