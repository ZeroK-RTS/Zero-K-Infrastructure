using PlasmaShared;
using Ratings;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
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

        public static IEnumerable<GraphPoint> FillHoles(IEnumerable<GraphPoint> source, DateTime from, DateTime to)
        {
            using (var enumerator = source.GetEnumerator())
            {
                enumerator.MoveNext();
                var dt = from;
                while (dt <= to)
                {
                    while (enumerator.Current != null && enumerator.Current.Day < dt) enumerator.MoveNext();
                    if (enumerator.Current?.Day == dt) yield return enumerator.Current;
                    else yield return new GraphPoint() { Day = dt, Value = 0 };

                    dt = dt.AddDays(1);
                }
            }
        }

    }


    public class Retention : IGraphDataProvider
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

            var selected = (from sbp in db.SpringBattlePlayers.Where(x => !x.IsSpectator)
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

            var selected = (from sbp in db.SpringBattlePlayers.Where(x => !x.IsSpectator)
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

            var selected = db.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => new { x.AccountID, x.SpringBattle.StartTime }).Where(x => x.StartTime >= fromTime && x.StartTime <= toTime).ToList();

            return (from sb in selected
                    group sb by sb.StartTime.Date
                into grp
                    orderby grp.Key
                    select
                    new GraphPoint()
                    {
                        Day = grp.Key,
                        Value = grp.Select(z => z.AccountID).Distinct().Count(),
                    }).OrderBy(x => x.Day).ToList();
        }

        public string Name => "daily_unique";
        public string Title => "daily unique players";
    }


    public class DailyUniqueSpec : IGraphDataProvider
    {
        public IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var selected = db.SpringBattlePlayers.Select(x => new { x.AccountID, x.SpringBattle.StartTime }).Where(x => x.StartTime >= fromTime && x.StartTime <= toTime).ToList();

            return (from sb in selected
                    group sb by sb.StartTime.Date
                into grp
                    orderby grp.Key
                    select
                    new GraphPoint()
                    {
                        Day = grp.Key,
                        Value = grp.Select(z => z.AccountID).Distinct().Count(),
                    }).OrderBy(x => x.Day).ToList();
        }

        public string Name => "daily_unique_spec";
        public string Title => "daily unique players and spectators";
    }


    public class DailyAvgMatchmakerMinutes : IGraphDataProvider
    {
        public IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var selected = db.SpringBattlePlayers.Where(x => !x.IsSpectator && x.SpringBattle.IsMatchMaker).Select(x => new { x.AccountID, x.SpringBattle.StartTime, x.SpringBattle.Duration }).Where(x => x.StartTime >= fromTime && x.StartTime <= toTime).ToList();


            return (from sb in selected
                    group sb by sb.StartTime.Date
                into grp
                    orderby grp.Key
                    let players = grp.Select(y => y.AccountID).Distinct().Count()
                    select new GraphPoint() { Day = grp.Key, Value = grp.Sum(y => y.Duration) / 60 / players, }).OrderBy(x => x.Day).ToList();
        }

        public string Name => "matchmaker_minutes";
        public string Title => "avg. matchmaker minutes per player";
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

    public class AverageGameSize : IGraphDataProvider
    {
        public IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime)
        {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var selected = db.SpringBattles.Select(x => new { x.PlayerCount, x.StartTime, x.Duration }).Where(x => x.StartTime >= fromTime && x.StartTime <= toTime).ToList();

            return (from sb in selected
                    group sb by sb.StartTime.Date
                into grp
                    orderby grp.Key
                    let playtime = grp.Sum(y => y.PlayerCount * y.Duration)
                    select new GraphPoint() { Day = grp.Key, Value = (double)grp.Sum(y => y.PlayerCount * y.PlayerCount * y.Duration) / playtime }).OrderBy(x => x.Day).ToList();
        }

        public string Name => "average_game_size";
        public string Title => "avg. game size by playtime";
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

    public class RatingHistory : IGraphDataProvider
    {
        public readonly int AccountID;
        public readonly RatingCategory Category;
        public readonly string AccountName;

        public RatingHistory(int accountID, RatingCategory category)
        {
            this.AccountID = accountID;
            this.Category = category;
            using (var db = new ZkDataContext())
            {
                AccountName = db.Accounts.Where(x => x.AccountID == accountID).FirstOrDefault().Name;
            }
        }

        public IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime)
        {
            Dictionary<DateTime, float> ratings = RatingSystems.GetRatingSystem(Category).GetPlayerRatingHistory(AccountID);
            return ratings.Where(x => x.Key >= fromTime && x.Key <= toTime).Select(x => new GraphPoint() { Day = x.Key, Value = x.Value, }).ToList();
        }

        public string Name => "rating_history";
        public string Title => AccountName ;
    }


    public class LadderRatingHistory : IGraphDataProvider
    {
        public readonly int AccountID;
        public readonly RatingCategory Category;
        public readonly string AccountName;

        public LadderRatingHistory(int accountID, RatingCategory category)
        {
            this.AccountID = accountID;
            this.Category = category;
            using (var db = new ZkDataContext())
            {
                AccountName = db.Accounts.Where(x => x.AccountID == accountID).FirstOrDefault().Name;
            }
        }

        public IList<GraphPoint> GetDailyValues(DateTime fromTime, DateTime toTime)
        {
            Dictionary<DateTime, float> ratings = RatingSystems.GetRatingSystem(Category).GetPlayerLadderRatingHistory(AccountID);
            return ratings.Where(x => x.Key >= fromTime && x.Key <= toTime).Select(x => new GraphPoint() { Day = x.Key, Value = x.Value, }).ToList();
        }

        public string Name => "ladder_rating_history";
        public string Title => AccountName + " (75% confidence)";
    }




    public class ChartsController : Controller
    {

        // GET: Charts
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult Index(ChartsModel model)
        {
            model = model ?? new ChartsModel();
            model.PossibleGraphs = GetPossibleProviders().Select(x => new ChartsModel.PossibleGraph() { Title = x.Title, Name = x.Name }).ToList();


            var to = model.To.Date;
            var from = model.From.Date;
            var grouping = model.Grouping;

            var providers = GetPossibleProviders().Where(x => model.Graphs.Contains(x.Name));

            var series = new List<GraphSeries>();

            foreach (var prov in providers)
            {
                var data = MemCache.GetCached($"chart_{prov.Title}_{from}_{to}", () => GraphPoint.FillHoles(prov.GetDailyValues(from, to), from, to).ToList(), 3600 * 24);


                if (grouping > 1)
                    data =
                        data.GroupBy(x => (int)x.Day.Subtract(from).TotalDays / grouping)
                            .Select(x => new GraphPoint() { Day = x.First().Day, Value = x.Average(y => y.Value) })
                            .ToList();

                if (data.Count > 0) data.RemoveAt(data.Count - 1);
                series.Add(new GraphSeries() { Title = prov.Title, Data = data });
            }


            if (model.Normalize)
                foreach (var s in series)
                {
                    var min = s.Data.Min(x => x.Value);
                    var max = s.Data.Max(x => x.Value);

                    foreach (var d in s.Data) d.Value = 100.0 * (d.Value - min) / (max - min);
                }

            model.GraphingData = series;
            return View("ChartsIndex", model);
        }

        private List<IGraphDataProvider> GetPossibleProviders()
        {
            List<IGraphDataProvider> providers = new List<IGraphDataProvider>()
            {

            };

            if (Global.Account?.AdminLevel >= AdminLevel.Moderator)
            {
                providers.AddRange(new List<IGraphDataProvider>()
                {
                    new Retention(),
                    new DailyUnique(),
                    new DailyUniqueSpec(),
                    new DailyNew(),
                    new RetentionLimit(1),
                    new RetentionLimit(3),
                    new RetentionLimit(7),
                    new RetentionLimit(30),
                    new DailyAvgMinutes(),
                    new DailyAvgMatchmakerMinutes(),
                    new AverageGameSize(),
                    new Leaving()
                });
            }
            return providers;
        }


        // GET: Charts/Ratings
        public ActionResult Ratings(ChartsModel model)
        {
            model = model ?? new ChartsModel();

            var to = model.To.Date;
            var from = model.From.Date;

            var providers = new List<IGraphDataProvider>();
            if (model.UserId != null)
            {
                providers.AddRange(model.UserId.Select(x => (IGraphDataProvider)new RatingHistory(x, model.RatingCategory)).ToList());
                //providers.AddRange(model.UserId.Select(x => (IGraphDataProvider)new LadderRatingHistory(x, model.RatingCategory)).ToList()); //75% confidence
            }

            var series = new List<GraphSeries>();

            foreach (var prov in providers)
            {
                series.Add(new GraphSeries() { Title = prov.Title, Data = prov.GetDailyValues(from, to) });
            }

            model.GraphingData = series;

            if (model.UserId != null)
            {
                using (var db = new ZkDataContext())
                {
                    model.UserStats = model.UserId.Select(id => new UserStats()
                    {
                        Account = db.Accounts.Where(x => x.AccountID == id).Include(x => x.Faction).Include(x => x.Clan).FirstOrDefault(),
                        RankStats = RatingSystems.ratingCategories.Select(s => new RankStats()
                        {
                            System = s.ToString(),
                            CurrentRating = RatingSystems.GetRatingSystem(s).GetPlayerRating(id),
                            Bracket = RatingSystems.GetRatingSystem(s).GetPercentileBracket(db.Accounts.FirstOrDefault(a => a.AccountID == id).Rank),
                        }).ToList(),
                    }).ToList();
                }
            }
            return View("ChartsRatings", model);
        }

        public class GraphSeries
        {
            public IList<GraphPoint> Data;
            public string Title;
        }

        public class ChartsModel
        {
            public List<PossibleGraph> PossibleGraphs = new List<PossibleGraph>();

            public int[] UserId { get; set; }
            public List<UserStats> UserStats { get; set; } = new List<UserStats>();
            public RatingCategory RatingCategory { get; set; } = RatingCategory.Casual;

            public DateTime From { get; set; } = DateTime.UtcNow.AddYears(-10).Date;

            public List<string> Graphs { get; set; } = new List<string>();

            public int Grouping { get; set; } = 1;

            public bool Normalize { get; set; }
            public DateTime To { get; set; } = DateTime.UtcNow.Date;

            public class PossibleGraph
            {
                public string Name;
                public string Title;
            }

            public IList<GraphSeries> GraphingData;

            public String[] Colors = { "e6194b", "3cb44b", "ffe119", "0082c8", "f58231", "911eb4", "46f0f0", "f032e6", "d2f53c", "fabebe", "008080", "e6beff", "aa6e28", "fffac8", "800000", "aaffc3", "808000", "ffd8b1", "000080", "808080", "FFFFFF", "000000" };
        }

        public class UserStats
        {
            public Account Account;
            public List<RankStats> RankStats;
        }

        public class RankStats
        {
            public string System;
            public PlayerRating CurrentRating;
            public RankBracket Bracket;
        }
    }
}