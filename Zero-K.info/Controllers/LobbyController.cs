using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LobbyClient;

namespace ZeroKWeb.Controllers
{
    public class LobbyController : Controller
    {
        //
        // GET: /Lobby/
        public ActionResult SendCommand(string link) {
            if (Global.Account == null) return Content("You must be logged in to the site");
            var name = Global.Account.Name;
            var tas = Global.Nightwatch.Tas;
            if (!tas.ExistingUsers.ContainsKey(name)) return Content("You need to start your lobby program first - Zero-K lobby or WebLobby");
            Global.Nightwatch.Tas.Extensions.SendJsonData(name, new ProtocolExtension.SiteToLobbyCommand{SpringLink = link});
            return Content("");
        }

    }
}
