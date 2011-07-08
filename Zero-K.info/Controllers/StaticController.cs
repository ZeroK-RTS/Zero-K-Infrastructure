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
          if (name == "UnitGuide") return View("Index", (object)"http://packages.springrts.com/zkmanual/index.html");

					return View("Index", (object)string.Format("~/{0}.inc", name));
        }

    }
}
