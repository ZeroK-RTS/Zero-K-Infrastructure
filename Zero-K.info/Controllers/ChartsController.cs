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
        string Label { get; }
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

            return (from acc in db.Accounts
                    where acc.SpringBattlePlayers.Any() && !acc.Name.StartsWith("TestNub") && (acc.FirstLogin >= fromTime) && (acc.FirstLogin <= toTime)
                    group acc by DbFunctions.TruncateTime(acc.FirstLogin)
                into x
                    orderby x.Key
                    select
                    new GraphPoint()
                    {
                        Day = x.Key.Value,
                        Value = x.Select(y => DbFunctions.DiffDays(x.Key, y.LastLogin)).Select(y => y > 30 ? 30 : y).Average() ?? 0
                    }).ToList();
        }

        public string Name => "retention";
        public string Label => "Avg. retention (cap 30)";
    }


    public class ChartsController : Controller
    {
        private List<IGraphDataProvider> GetPossibleProviders()
        {
            return new List<IGraphDataProvider>() {new Retention()};
        }


        public ActionResult GenerateGraph(ChartsModel model)
        {
            model = model ?? new ChartsModel();

            var to = model.To.Date;
            var from = model.From.Date;
            var grouping = model.Grouping;

            var providers = GetPossibleProviders().Where(x => model.Graphs.Contains(x.Name));
            

            var chart = new Chart(1500, 700, ChartTheme.Blue);
            chart.AddTitle(string.Join(", ", providers.Select(x => x.Name)));
            chart.AddLegend("Daily values", "l");
            var graphType = "Line";

            foreach (var prov in providers)
            {
                var data = MemCache.GetCached($"chart_{prov.Label}_{from}_{to}", () => prov.GetDailyValues(from, to), 3600 * 24);

                if (grouping > 1)
                    data =
                        data.GroupBy(x => (int)x.Day.Subtract(from).TotalDays / grouping)
                            .Select(x => new GraphPoint() { Day = x.First().Day, Value = x.Average(y => y.Value) })
                            .ToList();

                chart.AddSeries(prov.Label,
                    graphType,
                    xValue: data.Select(x => x.Day.Date.ToString("d")).ToList(),
                    yValues: data.Select(y => y.Value).ToList(),
                    legend: "l");
            }

            return File(chart.GetBytes("png"), "image/png");
        }

        // GET: Charts
        public ActionResult Index(ChartsModel model)
        {
            model = model ?? new ChartsModel();
            model.PossibleGraphs = GetPossibleProviders().Select(x => x.Name).ToList();
            return View("ChartsIndex", model);
        }

        public class ChartsModel
        {
            public DateTime From { get; set; } = DateTime.UtcNow.AddYears(-1).Date;

            public List<string> Graphs { get; set; } = new List<string>();

            public List<string> PossibleGraphs { get; set; } = new List<string>();

            public int Grouping { get; set; } = 1;
            public DateTime To { get; set; } = DateTime.UtcNow.Date;
        }
    }
}