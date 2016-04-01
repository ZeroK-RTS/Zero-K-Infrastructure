using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using System.Web.Mvc;
using Microsoft.Linq.Translations;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class LaddersController : Controller
    {
        public class GameStats
        {
            public DateTime Day { get; set; }
            public int PlayersAndSpecs { get; set; }
            public int MinutesPerPlayer { get; set; }
            public int FirstGamePlayers { get; set; }
        }

        /// <summary>
        /// Gets a chart of game activity since February 2011
        /// </summary>
	    [OutputCache(Duration = 3600 * 2)]
        public ActionResult Games()
        {

            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var data = MemCache.GetCached(
                "gameStats",
                () =>
                {
                    var start = new DateTime(2011, 2, 3);
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
                                PlayersAndSpecs = x.SelectMany(y => y.SpringBattlePlayers).Select(z => z.AccountID).Distinct().Count(),
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

            chart.AddSeries("unique players+specs", "Line", xValue: data.Select(x => x.Day).ToList(), yValues: data.Select(x => x.PlayersAndSpecs).ToList(), legend: "dps");
            chart.AddSeries("minutes/player", "Line", xValue: data.Select(x => x.Day).ToList(), yValues: data.Select(x => x.MinutesPerPlayer).ToList(), legend: "dps");
            chart.AddSeries("new players", "Line", xValue: data.Select(x => x.Day).ToList(), yValues: data.Select(x => x.FirstGamePlayers).ToList(), legend: "dps");

            return File(chart.GetBytes("png"), "image/png");
        }

        /// <summary>
        /// Returns information for ladder, awards hall of fame page
        /// </summary>
        /// <returns></returns>
        private LadderModel GetLadder() {
            var db = new ZkDataContext();
            db.Database.CommandTimeout = 600;

            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var validAwards =
                db.SpringBattles.Where(x => x.StartTime >= monthStart && !x.ResourceByMapResourceID.InternalName.Contains("SpeedMetal"))
                    .SelectMany(x => x.AccountBattleAwards)
                    .GroupBy(x => x.AwardKey);

            var awardItems = new List<AwardItem>();

            foreach (var awardsByType in validAwards)
            {
                var awardType = awardsByType.Key;

                var awardCounts =
                    awardsByType.GroupBy(x => x.Account).Select(x => new { Account = x.Key, Count = x.Count() }).OrderByDescending(x => x.Count);

                var topCountM = awardCounts.First().Count;
                var topCollectorsM = new List<Account>();
                foreach (var award in awardCounts)
                {
                    if (award.Count == topCountM) topCollectorsM.Add(award.Account);
                    else break;
                }

                var topScore = 0;
                string titleName = null;

                topScore = 0;
                var fullTitleM = "";
                var topActID = 0;
                var topBattleID = 0;
                foreach (var award in awardsByType)
                {
                    if (titleName == null) titleName = award.AwardDescription.Split(',').First();
                    int score;
                    if (int.TryParse(Regex.Replace(award.AwardDescription, @"\D", string.Empty), out score))
                    {
                        if (score > topScore)
                        {
                            topActID = award.AccountID;
                            topBattleID = award.SpringBattleID;
                            topScore = score;
                            fullTitleM = string.Join(" ", award.AwardDescription.Split(',').Skip(1));
                        }
                    }
                }

                var awardItem = new AwardItem
                {
                    AwardType = awardType,
                    AwardTitle = titleName,
                    TopScoreHolderM = db.Accounts.SingleOrDefault(x => x.AccountID == topActID),
                    TopScoreDescM = fullTitleM,
                    TopScoreBattlePlayerM = db.SpringBattlePlayers.SingleOrDefault(x => x.AccountID == topActID && x.SpringBattleID == topBattleID),
                    TopCollectorsM = topCollectorsM,
                    TopCollectorCountM = topCountM
                };
                awardItems.Add(awardItem);
            }

            var ladderTimeout = DateTime.UtcNow.AddDays(-GlobalConst.LadderActivityDays);
            var top50Accounts =
                db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > ladderTimeout && y.SpringBattle.PlayerCount == 2 && y.SpringBattle.HasBots == false && y.EloChange != null && Math.Abs(y.EloChange) > 0 && !y.IsSpectator))
                    .Include(x => x.Clan)
                    .Include(x => x.Faction)
                    .OrderByDescending(x => x.Effective1v1Elo)
                    .WithTranslations()
                    .Take(50)
                    .ToList();

            var top50Teams =
                db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > ladderTimeout && y.SpringBattle.PlayerCount > 2 && y.SpringBattle.HasBots == false && y.EloChange != null && Math.Abs(y.EloChange) > 0 && !y.IsSpectator))
                    .Include(x => x.Clan)
                    .Include(x => x.Faction)
                    .OrderByDescending(x => x.EffectiveElo)
                    .WithTranslations()
                    .Take(50)
                    .ToList();

            return new LadderModel { AwardItems = awardItems, Top50Accounts = top50Accounts, Top50Teams = top50Teams };
        }
        //
        // GET: /Ladders/
        public ActionResult Index()
        {
            var ladderModel = MemCache.GetCached("ladderModel", GetLadder, 60*60*2);
            return View("Ladders", ladderModel);
        }


        public class AwardItem
        {
            public string AwardTitle;
            public string AwardType;

            //for all time
            public int TopCollectorCount;


            //for this month
            public int TopCollectorCountM;
            public List<Account> TopCollectors;
            public List<Account> TopCollectorsM;
            public SpringBattlePlayer TopScoreBattlePlayer;
            public SpringBattlePlayer TopScoreBattlePlayerM;
            public string TopScoreDesc;
            public string TopScoreDescM;
            public Account TopScoreHolder;
            public Account TopScoreHolderM;
        }

        public class LadderModel
        {
            public List<AwardItem> AwardItems;
            public List<Account> Top50Accounts;
            public List<Account> Top50Teams;
        }
    }
}
