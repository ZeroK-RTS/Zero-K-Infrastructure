using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
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

  
        public ActionResult Games()
        {
            return GenerateStats(1);
        }

        public ActionResult GamesAll()
        {
            return GenerateStats(10);
        }


        private ActionResult GenerateStats(int years)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var data = MemCache.GetCached("gameStats" + years,
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
                            Players = x.SelectMany(y => y.SpringBattlePlayers.Where(z => !z.IsSpectator)).Select(z => z.AccountID).Distinct().Count(),
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
            chart.AddSeries("minutes/player",
                "Line",
                xValue: data.Select(x => x.Day).ToList(),
                yValues: data.Select(x => x.MinutesPerPlayer).ToList(),
                legend: "dps");
            chart.AddSeries("new players",
                "Line",
                xValue: data.Select(x => x.Day).ToList(),
                yValues: data.Select(x => x.FirstGamePlayers).ToList(),
                legend: "dps");

            return File(chart.GetBytes("png"), "image/png");
        }


        public ActionResult Cohorts(int? years)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;
            years = years ?? 1;

            var data = MemCache.GetCached("cohorts" + years,
                () =>
                {
                    var start = DateTime.Now.AddYears(-years.Value); //new DateTime(2011, 2, 3);
                    var end = DateTime.Now.Date.AddDays(-30);

                    return (from acc in db.Accounts
                            where acc.FirstLogin < end && acc.FirstLogin > start
                            group acc by DbFunctions.TruncateTime(acc.FirstLogin)
                        into x
                            orderby x.Key
                            let players = x.Count()
                            select
                            new 
                            {
                                Day = x.Key.Value,
                                Players = x.Count(),
                                Day1 = x.Count(y=>y.LastLogin>= DbFunctions.AddDays(x.Key, 1)),
                                Day3 = x.Count(y => y.LastLogin >= DbFunctions.AddDays(x.Key, 3)),
                                Day7 = x.Count(y => y.LastLogin >= DbFunctions.AddDays(x.Key, 7)),
                                Day30 = x.Count(y => y.LastLogin >= DbFunctions.AddDays(x.Key, 30)),
                            }).ToList();
                },
                60 * 60 * 20);

            var chart = new Chart(1500, 700, ChartTheme.Blue);

            chart.AddTitle("Cohorts");
            chart.AddLegend("Daily values", "dps");

            //chart.AddSeries("New players", "Line", xValue: data.Select(x => x.Day).ToList(), yValues: data.Select(x => x.Players).ToList(), legend: "dps");

            var t = "SplineArea";

            chart.AddSeries("1 day", t, xValue: data.Select(x => x.Day).ToList(), yValues: data.Select(x => 100.0 * x.Day1 / x.Players).ToList(), legend: "dps");
            chart.AddSeries("3 days", t, xValue: data.Select(x => x.Day).ToList(), yValues: data.Select(x => 100.0 * x.Day3 / x.Players).ToList(), legend: "dps");
            chart.AddSeries("7 days", t, xValue: data.Select(x => x.Day).ToList(), yValues: data.Select(x => 100.0 * x.Day7 / x.Players).ToList(), legend: "dps");
            chart.AddSeries("30 days", t, xValue: data.Select(x => x.Day).ToList(), yValues: data.Select(x => 100.0 * x.Day30 / x.Players).ToList(), legend: "dps");
            return File(chart.GetBytes("png"), "image/png");
        }

        public ActionResult Retention(int? years)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;
            years = years ?? 1;

            var start = DateTime.Now.AddYears(-years.Value); //new DateTime(2011, 2, 3);

            var data = MemCache.GetCached("retention" + years,
                () =>
                {
                    var end = DateTime.Now.Date.AddDays(-30);

                    return (from acc in db.Accounts
                            where acc.FirstLogin < end && acc.FirstLogin > start
                            group acc by DbFunctions.TruncateTime(acc.FirstLogin)
                        into x
                            orderby x.Key
                            let players = x.Count()
                            select
                            new
                            {
                                Day = x.Key.Value,
                                Players = x.Count(),
                                Retention = x.Select(y => DbFunctions.DiffDays(x.Key, y.LastLogin)).Select(y => y > 30 ? 30 : y).Average(),
                            }).ToList();
                },
                60 * 60 * 20);

            var chart = new Chart(1500, 700, ChartTheme.Blue);

            chart.AddTitle("Retention (max 30)");
            chart.AddLegend("Daily values", "dps");

            var t = "SplineArea";

            chart.AddSeries("Days (up to 30)", t, xValue: data.GroupBy(x=> (int)x.Day.Subtract(start).TotalDays/7).Select(x=>x.First().Day).ToList(), yValues: data.GroupBy(x => (int)x.Day.Subtract(start).TotalDays / 7).Select(x => x.Average(y=>y.Retention)).ToList(), legend: "dps");
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
