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

        public ActionResult Index(string id = "tremor") {
            var db = new ModStatsDb();
            var unit = id;
            var gameFilter = db.Games.OrderByDescending(x=>x.GameID).Take(500);
            var results = new List<MonthEntry>();

            foreach (var m in gameFilter.GroupBy(x => x.Created.Year * 13 + x.Created.Month)) {
                var gameIds = m.Select(x => x.GameID).ToList();
                var units = db.Units.Where(x=>gameIds.Contains(x.GameID));
                var damages = db.Damages.Where(x => gameIds.Contains(x.GameID));
                var totalSpending = units.Sum(x => (double?)x.Cost*x.Created);
                if (totalSpending == 0) continue;
                ;
                var unitCostHealth = units.GroupBy(x => x.Unit1).ToDictionary(x => x.Key, x => x.Select(y => (double?)y.Cost/y.Health).FirstOrDefault());

                if (!unitCostHealth.ContainsKey(unit)) continue;
                var unitDamageRecieved = damages.Where(x => x.VictimUnit == unit).Sum(x => (double?)x.Damage1);
                
                var costDamageRecieved = unitDamageRecieved*unitCostHealth[unit];
                var costDamageDone = damages.Where(x => x.AttackerUnit == unit).Sum(x => x.Damage1*unitCostHealth[x.VictimUnit]);

                results.Add(new MonthEntry() { Month = new DateTime(m.Key/13, m.Key%13, 1), CostDamagedLost = costDamageDone/costDamageRecieved, CostDamagedInvest = costDamageDone/totalSpending });
            }

            var data = results.OrderBy(x => x.Month);
            var chart = new Chart(1500, 700, ChartTheme.Blue);

            chart.AddTitle(string.Format("{0} effectivity", unit));
            chart.AddLegend("Monthly averages", "ma");

            chart.AddSeries("Cost damaged/lost", "Line", xValue: data.Select(x => x.Month), yValues: data.Select(x => x.CostDamagedLost), legend: "ma");
            chart.AddSeries("Cost damaged/invest", "Line", xValue: data.Select(x => x.Month), yValues: data.Select(x => x.CostDamagedInvest), legend: "ma");

            return File(chart.GetBytes("png"), "image/png");
        }
    }

    public class MonthEntry
    {
        public double? CostDamagedInvest;
        public double? CostDamagedLost;
        public DateTime Month;
    }
}