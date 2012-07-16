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

        [Auth]
        public ActionResult JoinFaction(int id)
        {
            if (Global.Account.FactionID != null) return Content("Already in faction");
            var db = new ZkDataContext();
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            acc.FactionID = id;
           
            var faction = db.Factions.Single(x => x.FactionID == id);
            db.Events.InsertOnSubmit(Global.CreateEvent("{0} joins {1}", acc, faction));
            db.SubmitChanges();
            return Content(string.Format("Done, welcome to the {0}!", faction.Name));
        }




        [Auth]
        public ActionResult LeaveFaction()
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            if (acc.Clan != null) ClansController.PerformLeaveClan(Global.AccountID);
            db.AccountRoles.DeleteAllOnSubmit(acc.AccountRolesByAccountID);
            acc.ResetQuotas();
            
            db.Events.InsertOnSubmit(Global.CreateEvent("{0} leaves faction {1}", acc, acc.Faction));
            db.SubmitChanges();
            db.Dispose();
            db = new ZkDataContext();
            var acc2 = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            acc2.FactionID = null;
            db.SubmitChanges();

            PlanetwarsController.SetPlanetOwners();
            return RedirectToAction("Index", "Clans");
        }

    }
}
