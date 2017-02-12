using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Helpers;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public interface IGraphDataProvider
    {
        string Name { get; }
        string Title { get; }
        IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime);
    }


    public class GraphPoint
    {
        public DateTime Day;
        public double Value;
    }


    public class Retention : IGraphDataProvider
    {
        public IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var selected = (from sbp in db.SpringBattlePlayers.Where(x=>!x.IsSpectator)
                join sb in db.SpringBattles on sbp.SpringBattleID equals sb.SpringBattleID select new { sb,sbp}).GroupBy(x => x.sbp.AccountID).Select(x => new
            {
                FirstLogin = x.Min(y=>y.sb.StartTime),
                LastLogin = x.Max(y=>y.sb.StartTime)
            }).Where(x => (x.FirstLogin >= fromTime) && (x.FirstLogin <= toTime)).ToList();

            return (from acc in selected
                    group acc by acc.FirstLogin.Date
                into grp
                    orderby grp.Key
                    select
                    new GraphPoint()
                    {
                        Day = grp.Key,
                        Value = grp.Select(y => y.LastLogin.Subtract(y.FirstLogin).TotalDays).Select(y => y > 30 ? 30 : y).Average()
                    }).OrderBy(x => x.Day).ToList();
        }

        public string Name => "retention";
        public string Title => "new player retention (avg days, cap 30)";
    }

    public class Leaving : IGraphDataProvider
    {
        public IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var selected = (from sbp in db.SpringBattlePlayers.Where(x=>!x.IsSpectator)
                            join sb in db.SpringBattles on sbp.SpringBattleID equals sb.SpringBattleID
                            select new { sb, sbp }).GroupBy(x => x.sbp.AccountID).Select(x => new
                            {
                                FirstLogin = x.Min(y => y.sb.StartTime),
                                LastLogin = x.Max(y => y.sb.StartTime)
                            }).Where(x => (x.LastLogin >= fromTime) && (x.LastLogin <= toTime)).ToList();

            return (from acc in selected
                    group acc by acc.LastLogin.Date
                into grp
                    orderby grp.Key
                    select
                    new GraphPoint()
                    {
                        Day = grp.Key,
                        Value = grp.Select(y => y.LastLogin.Subtract(y.FirstLogin).TotalDays).Select(y => y > 30 ? 30 : y).Average()
                    }).OrderBy(x => x.Day).ToList();
        }

        public string Name => "leavers";
        public string Title => "leaver age (avg days, cap 30)";
    }

    public class RetentionLimit : IGraphDataProvider
    {
        private int limitDays;

        public RetentionLimit(int limitDays)
        {
            this.limitDays = limitDays;
        }

        public IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var selected = (from sbp in db.SpringBattlePlayers.Where(x=>!x.IsSpectator)
                            join sb in db.SpringBattles on sbp.SpringBattleID equals sb.SpringBattleID
                            select new { sb, sbp }).GroupBy(x => x.sbp.AccountID).Select(x => new
                            {
                                FirstLogin = x.Min(y => y.sb.StartTime),
                                LastLogin = x.Max(y => y.sb.StartTime)
                            }).Where(x => (x.FirstLogin >= fromTime) && (x.FirstLogin <= toTime)).ToList();

            return (from acc in selected
                    group acc by acc.FirstLogin.Date
                into grp
                    orderby grp.Key
                    select
                    new GraphPoint()
                    {
                        Day = grp.Key,
                        Value = 100.0 * grp.Count(x => x.LastLogin.Subtract(x.FirstLogin).TotalDays >= limitDays) / grp.Count()
                    }).OrderBy(x => x.Day).ToList();
        }

        public string Name => "retention_" + limitDays;
        public string Title => "new player retention " + limitDays + " days (%)";
    }


    public class DailyUnique : IGraphDataProvider
    {
        public IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var selected = db.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => new { x.AccountID, x.SpringBattle.StartTime }).Where(x=>x.StartTime >= fromTime && x.StartTime <= toTime).ToList();
            
            return (from sb in selected
                    group sb by sb.StartTime.Date
                into grp
                    orderby grp.Key
                    select
                    new GraphPoint()
                    {
                        Day = grp.Key,
                        Value = grp.Select(z => z.AccountID).Distinct().Count(),
                    }).OrderBy(x=>x.Day).ToList();
        }

        public string Name => "daily_unique";
        public string Title => "daily unique players";
    }


    public class DailyAvgMinutes : IGraphDataProvider
    {
        public IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var selected = db.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => new { x.AccountID, x.SpringBattle.StartTime, x.SpringBattle.Duration }).Where(x => x.StartTime >= fromTime && x.StartTime <= toTime).ToList();


            return (from sb in selected
                    group sb by sb.StartTime.Date
                into grp
                    orderby grp.Key
                    let players = grp.Select(y => y.AccountID).Distinct().Count()
                    select new GraphPoint() { Day = grp.Key, Value = grp.Sum(y => y.Duration) / 60 / players, }).OrderBy(x => x.Day).ToList();
        }

        public string Name => "player_minutes";
        public string Title => "avg. player minutes per player";
    }


    public class DailyNew : IGraphDataProvider
    {
        public IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var selected = (from sbp in db.SpringBattlePlayers.Where(x => !x.IsSpectator)
                            join sb in db.SpringBattles on sbp.SpringBattleID equals sb.SpringBattleID
                            select new { sb, sbp }).GroupBy(x => x.sbp.AccountID).Select(x => new
                            {
                                FirstLogin = x.Min(y => y.sb.StartTime),
                            }).Where(x => (x.FirstLogin >= fromTime) && (x.FirstLogin <= toTime)).ToList();

            return (from acc in selected
                    group acc by acc.FirstLogin.Date
                into grp
                    orderby grp.Key
                    select new GraphPoint() { Day = grp.Key, Value = grp.Count(), }).OrderBy(x => x.Day).ToList();
        }

        public string Name => "daily_first";
        public string Title => "daily first-time players";
    }


    [Auth(Role = AuthRole.ZkAdmin)]
    public class ChartsController : Controller
    {
        public ActionResult GenerateGraph(ChartsModel model)
        {
            model = model ?? new ChartsModel();

            var to = model.To.Date;
            var from = model.From.Date;
            var grouping = model.Grouping;

            var providers = GetPossibleProviders().Where(x => model.Graphs.Contains(x.Name));

            var series = new List<GraphSeries>();

            foreach (var prov in providers)
            {
                var data = MemCache.GetCached($"chart_{prov.Title}_{from}_{to}", () => prov.GetDailyValues(from, to), 3600 * 24);

                if (grouping > 1)
                    data =
                        data.GroupBy(x => (int)x.Day.Subtract(from).TotalDays / grouping)
                            .Select(x => new GraphPoint() { Day = x.First().Day, Value = x.Average(y => y.Value) })
                            .ToList();

                series.Add(new GraphSeries() { Title = prov.Title, Data = data });
            }
            if (model.Normalize)
                foreach (var s in series)
                {
                    var min = s.Data.Min(x => x.Value);
                    var max = s.Data.Max(x => x.Value);

                    foreach (var d in s.Data) d.Value = 100.0 * (d.Value - min) / (max - min);
                }

            // TODO: convert this to System.Web.UI.DataVisualization.Charting  (which this thing is internally using)

            var chart = new Chart(1500, 700, ChartTheme.Blue);
            chart.AddTitle(string.Join(", ", providers.Select(x => x.Name)));
            chart.AddLegend("Daily values", "l");
            var graphType = "Line";

            foreach (var s in series)
                chart.AddSeries(s.Title,
                    graphType,
                    xValue: s.Data.Select(x => x.Day.Date.ToString("d")).ToList(),
                    yValues: s.Data.Select(y => y.Value).ToList(),
                    legend: "l");

            return File(chart.GetBytes("png"), "image/png");
        }

        // GET: Charts
        public ActionResult Index(ChartsModel model)
        {
            model = model ?? new ChartsModel();
            model.PossibleGraphs = GetPossibleProviders().Select(x => new ChartsModel.PossibleGraph() { Title = x.Title, Name = x.Name }).ToList();
            return View("ChartsIndex", model);
        }

        private List<IGraphDataProvider> GetPossibleProviders()
        {
            return new List<IGraphDataProvider>()
            {
                new Retention(),
                new DailyUnique(),
                new DailyNew(),
                new RetentionLimit(1),
                new RetentionLimit(3),
                new RetentionLimit(7),
                new RetentionLimit(30),
                new DailyAvgMinutes(),
                new Leaving()
            };
        }

        public class GraphSeries
        {
            public IList<GraphPoint> Data;
            public string Title;
        }

        public class ChartsModel
        {
            public List<PossibleGraph> PossibleGraphs = new List<PossibleGraph>();

            public DateTime From { get; set; } = DateTime.UtcNow.AddYears(-1).Date;

            public List<string> Graphs { get; set; } = new List<string>();

            public int Grouping { get; set; } = 1;

            public bool Normalize { get; set; }
            public DateTime To { get; set; } = DateTime.UtcNow.Date;

            public class PossibleGraph
            {
                public string Name;
                public string Title;
            }
        }
    }
}