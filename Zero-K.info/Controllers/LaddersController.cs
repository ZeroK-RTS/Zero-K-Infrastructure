﻿using System;
using System.Collections.Generic;
﻿using System.Data.Linq.SqlClient;
﻿using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
	public class LaddersController: Controller
	{
        [OutputCache(Duration = 60 * 60 * 24, VaryByParam = "none")] // Cache for a day
		public ActionResult Games()
		{
			var db = new ZkDataContext();
			var data = from bat in db.SpringBattles
			           where bat.StartTime.Date < DateTime.Now.Date  && bat.StartTime.Date > new DateTime(2011,2,3)
			           group bat by bat.StartTime.Date
			           into x orderby x.Key
			           let players = x.SelectMany(y => y.SpringBattlePlayers.Where(z => !z.IsSpectator)).Select(z => z.AccountID).Distinct().Count()
			           select
			           	new
			           	{
			           		Day = x.Key,
			           		PlayersAndSpecs = x.SelectMany(y => y.SpringBattlePlayers).Select(z => z.AccountID).Distinct().Count(),
			           		//Players = players,
                            PwPlayers = x.Where(y => SqlMethods.Like(y.Account.Name,"PlanetWars%")).SelectMany(y => y.SpringBattlePlayers).Select(z => z.AccountID).Distinct().Count(),
			           		MinutesPerPlayer = x.Sum(y => y.Duration*y.PlayerCount)/60/players,
										FirstGamePlayers = x.SelectMany(y=>y.SpringBattlePlayers).GroupBy(y=>y.Account).Where(y=>y.Any(z=>z == y.Key.SpringBattlePlayers.First())).Count()
			           	};

			var chart = new Chart(1500, 700, ChartTheme.Blue);

			chart.AddTitle("Daily activity");
			chart.AddLegend("Daily values", "dps");

			chart.AddSeries("unique players+specs", "Line", xValue: data.Select(x => x.Day), yValues: data.Select(x => x.PlayersAndSpecs), legend: "dps");
            chart.AddSeries("PW players+specs", "Line", xValue: data.Select(x => x.Day), yValues: data.Select(x => x.PwPlayers), legend: "dps");
			//chart.AddSeries("unique players", "Line", xValue: data.Select(x => x.Day), yValues: data.Select(x => x.Players), legend: "dps");
			chart.AddSeries("minutes/player", "Line", xValue: data.Select(x => x.Day), yValues: data.Select(x => x.MinutesPerPlayer), legend: "dps");
			chart.AddSeries("new players", "Line", xValue: data.Select(x => x.Day), yValues: data.Select(x => x.FirstGamePlayers), legend: "dps");

            return File(chart.GetBytes("png"), "image/png");
		}

		//
		// GET: /Ladders/
		[OutputCache(Duration = 3600*2, VaryByCustom = GlobalConst.LobbyAccessCookieName)] // cache for 2 hours - different look for lobby and for normal
		public ActionResult Index()
		{
			var db = new ZkDataContext();
            db.CommandTimeout = 300;

            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var validAwards = db.SpringBattles.Where(x => x.StartTime >= monthStart && !x.ResourceByMapResourceID.InternalName.Contains("SpeedMetal")).SelectMany(x => x.AccountBattleAwards).GroupBy(x => x.AwardKey);

			var awardItems = new List<AwardItem>();
            
			foreach (var awardsByType in validAwards)
			{
				var awardType = awardsByType.Key;

                var awardCounts = awardsByType.GroupBy(x => x.Account).Select(x => new { Account = x.Key, Count = x.Count()}).OrderByDescending(x=>x.Count);

                var topCountM = awardCounts.First().Count;
                var topCollectorsM = new List<Account>();
                foreach (var award in awardCounts) {
                    if (award.Count == topCountM) topCollectorsM.Add(award.Account);
                    else break;
                }
			    


				var topScore = 0;
                var titleName = "";

                topScore = 0;
				var fullTitleM = "";
                int topActID = 0;
                int topBattleID = 0;
                foreach (var award in awardsByType)
				{
					var score = Convert.ToInt32(Regex.Replace(award.AwardDescription, @"\D", String.Empty));
                    titleName = award.AwardDescription.Split(',').First();
                    
					if (score > topScore)
					{
                        topActID = award.AccountID;
                        topBattleID = award.SpringBattleID;
                        topScore = score;
						fullTitleM = string.Join(" ", award.AwardDescription.Split(',').Skip(1));
					}

                    
				}

				var awardItem = new AwardItem
				                {
				                	AwardType = awardType,
				                	AwardTitle = titleName,
				                	TopScoreHolderM = db.Accounts.Single(x=>x.AccountID == topActID),
				                	TopScoreDescM = fullTitleM,
				                	TopScoreBattlePlayerM = db.SpringBattlePlayers.Single(x=>x.AccountID == topActID && x.SpringBattleID == topBattleID),
				                	TopCollectorsM = topCollectorsM,
				                	TopCollectorCountM = topCountM,
				                };
				awardItems.Add(awardItem);
			}

			var top50Accounts =
				db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > DateTime.UtcNow.AddMonths(-1))).OrderByDescending(x => x.Elo).
					Take(50);

			var ladderModel = new LadderModel { AwardItems = awardItems, Top50Accounts = top50Accounts };
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
			public IQueryable<Account> Top50Accounts;
		}
	}
}