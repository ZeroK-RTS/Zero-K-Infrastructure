using System.IO;
using System.Web.Mvc;
using LobbyClient;
using Newtonsoft.Json.Linq;

namespace ZeroKWeb.Controllers
{
    public class GithubController : Controller
    {
        // GET: Github
        [HttpPost]
        public ActionResult Hook()
        {
            var eventType = Request.Headers["X-Github-Event"];
            var signature = Request.Headers["X-Hub-Signature"]; // todo verify signature
            

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