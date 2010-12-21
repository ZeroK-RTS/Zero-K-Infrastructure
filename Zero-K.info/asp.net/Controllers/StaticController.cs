using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ZeroKWeb.Controllers
{
    public class StaticController : Controller
    {
        //
        // GET: /Static/

        public ActionResult Index(string name = "LobbyStart")
        {
          if (name == "LobbyStart") return View("Index", (object)"LobbyStart.inc");
          else return Content("Invalid page");
        }

    }
}
