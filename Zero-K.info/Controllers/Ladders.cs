using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ZeroKWeb.Controllers
{
    public class Ladders : Controller
    {
        //
        // GET: /Ladders/

        public ActionResult Index()
        {
            return View("Ladders");
        }

    }
}
