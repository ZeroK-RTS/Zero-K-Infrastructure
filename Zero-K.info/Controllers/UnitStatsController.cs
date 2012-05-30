using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Helpers;
using System.Web.Mvc;
using ZeroKWeb.AppCode;

namespace ZeroKWeb.Controllers
{
    public class UnitStatsController: Controller
    {
        //
        // GET: /UnitStats/

        public ActionResult Index(string id = "trem") {
            var db = new ModStatsDb();
            var unit = id;
            var gameFilter = db.Games;
            var results = new List<MonthEntry>();

            ViewBag.Unit = unit;


            if (!db.Units.Any(x => x.Unit1 == unit)) return Content("Unit not found");

            foreach (var m in gameFilter.GroupBy(x => x.Created.Year * 13 + x.Created.Month)) {
                var gameIds = m.Select(x => x.GameID).ToList();
                var units = db.Units.Where(x=>gameIds.Contains(x.GameID));
                var damages = db.Damages.Where(x => gameIds.Contains(x.GameID));
                var totalSpending = units.Where(x=>x.Unit1== unit).Sum(x => (double?)x.Cost*x.Created);
                if (totalSpending == 0) continue;
                ;
                var unitCostHealth = units.GroupBy(x => x.Unit1).ToDictionary(x => x.Key, x => x.Select(y => (double?)y.Cost/y.Health).FirstOrDefault());

                if (!unitCostHealth.ContainsKey(unit)) continue;
                var unitDamageRecieved = damages.Where(x => x.VictimUnit == unit).Sum(x => (double?)x.Damage1);
                if (unitDamageRecieved == null) continue;

                var costDamageRecieved = unitDamageRecieved*unitCostHealth[unit];
                double costDamageDone = 0;
                foreach (var dd in damages.Where(x => x.AttackerUnit == unit).GroupBy(x=>x.VictimUnit).Select(x=>new {unit = x.Key, sum =x.Sum(y=>y.Damage1)})) costDamageDone +=dd.sum * (unitCostHealth[dd.unit]??0);
                

                results.Add(new MonthEntry() { Month = new DateTime(m.Key/13, m.Key%13, 1), CostDamagedLost = (costDamageDone/costDamageRecieved)??0, CostDamagedInvest = (costDamageDone/totalSpending)??0 });
            }

            var data = results.OrderBy(x => x.Month).ToList();
            if (!data.Any()) return Content("Set empty");
            return View("UnitStatsDetail", data);
            
        }
    }

    public class MonthEntry
    {
        public double? CostDamagedInvest;
        public double? CostDamagedLost;
        public DateTime Month;
    }
}