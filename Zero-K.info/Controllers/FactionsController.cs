using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class FactionsController : Controller
    {
        //
        // GET: /Factions/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Detail(int id) {
            return View(new ZkDataContext().Factions.Single(x => x.FactionID == id));
        }
    }
}
