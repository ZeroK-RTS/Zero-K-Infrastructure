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
          var db = new ZkDataContext();
          var bat = db.SpringBattles.Single(x => x.SpringBattleID == id);
          if (bat.ForumThread != null)
          {
            bat.ForumThread.UpdateLastRead(Global.AccountID, false);
            db.SubmitChanges();
          }
          return View(bat);

        }

    }
}
