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
            var signature = Request.Headers["X-Hub-Signature"].Substring(5);


            var ms = new MemoryStream();
            Request.InputStream.CopyTo(ms);
            byte[] data = ms.ToArray();

            var secretKey = new Secrets().GetGithubHookKey(new ZkDataContext());
            var hash = new HMACSHA1(Encoding.ASCII.GetBytes(secretKey)).ComputeHash(data);

            if (!string.Equals(hash.ToHex(), signature, StringComparison.InvariantCultureIgnoreCase)) return Content("Signature does not match");
            
            dynamic payload = JObject.Parse(Encoding.UTF8.GetString(data));

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