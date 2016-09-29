using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.UI;
using Microsoft.Linq.Translations;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class LaddersController : Controller
    {
        public class GameStats
        {
            public DateTime Day { get; set; }
            public int Players { get; set; }
            public int MinutesPerPlayer { get; set; }
            public int FirstGamePlayers { get; set; }
        }

        /// <summary>
        /// Gets a chart of game activity since February 2011
        /// </summary>
	    [OutputCache(Duration = 3600 * 2, VaryByParam = "*", Location = OutputCacheLocation.Server)]
        public ActionResult Games(int years = 1)
        {

            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var data = MemCache.GetCached(
                "gameStats",
                () =>
                {
                    var start = DateTime.Now.AddYears(-years); //new DateTime(2011, 2, 3);
                    var end = DateTime.Now.Date;
                    return (from bat in db.SpringBattles
                        where bat.StartTime < end && bat.StartTime > start
                        group bat by DbFunctions.TruncateTime(bat.StartTime)
                        into x
                        orderby x.Key
                        let players = x.SelectMany(y => y.SpringBattlePlayers.Where(z => !z.IsSpectator)).Select(z => z.AccountID).Distinct().Count()
                        select
                            new GameStats
                            {
                                Day = x.Key.Value,
                                Players = x.SelectMany(y => y.SpringBattlePlayers.Where(z=>!z.IsSpectator)).Select(z => z.AccountID).Distinct().Count(),
                                MinutesPerPlayer = x.Sum(y => y.Duration*y.PlayerCount)/60/players,
                                FirstGamePlayers =
                                    x.SelectMany(y => y.SpringBattlePlayers)
                                        .GroupBy(y => y.Account)
                                        .Count(y => y.Any(z => z == y.Key.SpringBattlePlayers.FirstOrDefault()))
                            }).ToList();
                },
                60*60*20);

            var chart = new Chart(1500, 700, ChartTheme.Blue);

            chart.AddTitle("Daily activity");
            chart.AddLegend("Daily values", "dps");

            chart.AddSeries("unique players", "Line", xValue: data.Select(x => x.Day).ToList(), yValues: data.Select(x => x.Players).ToList(), legend: "dps");
            chart.AddSeries("minutes/player", "Line", xValue: data.Select(x => x.Day).ToList(), yValues: data.Select(x => x.MinutesPerPlayer).ToList(), legend: "dps");
            chart.AddSeries("new players", "Line", xValue: data.Select(x => x.Day).ToList(), yValues: data.Select(x => x.FirstGamePlayers).ToList(), legend: "dps");

            return File(chart.GetBytes("png"), "image/png");
        }


        //
        // GET: /Ladders/
        public ActionResult Index()
        {
            return View("Ladders", Global.LadderCalculator.GetLadder());
        }
    }
}
