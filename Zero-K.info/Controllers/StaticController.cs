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

          if (System.IO.File.Exists(Server.MapPath(name+".inc")))
          {
            return View("Index", (object)(name+".inc"));
          }
          
          return Content("Invalid page");
        }

    }
}
