using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class BattlesController : Controller
    {
        //
        // GET: /Battles/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Detail(int id)
        {
          return View(new ZkDataContext().SpringBattles.Single(x => x.SpringBattleID == id));

        }

    }
}
