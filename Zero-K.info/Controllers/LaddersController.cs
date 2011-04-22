﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
	public class LaddersController: Controller
	{
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
			           		Players = players,
			           		MinutesPerPlayer = x.Sum(y => y.Duration*y.PlayerCount)/60/players,
										FirstGamePlayers = x.SelectMany(y=>y.SpringBattlePlayers).GroupBy(y=>y.Account).Where(y=>y.Any(z=>z == y.Key.SpringBattlePlayers.First())).Count()
			           	};

			var chart = new Chart(1500, 700, ChartTheme.Blue);

			chart.AddTitle("Daily activity");
			chart.AddLegend("Daily values", "dps");

			chart.AddSeries("unique players+specs", "Line", xValue: data.Select(x => x.Day), yValues: data.Select(x => x.PlayersAndSpecs), legend: "dps");
			chart.AddSeries("unique players", "Line", xValue: data.Select(x => x.Day), yValues: data.Select(x => x.Players), legend: "dps");
			chart.AddSeries("minutes/player", "Line", xValue: data.Select(x => x.Day), yValues: data.Select(x => x.MinutesPerPlayer), legend: "dps");
			chart.AddSeries("new players", "Line", xValue: data.Select(x => x.Day), yValues: data.Select(x => x.FirstGamePlayers), legend: "dps");

			return File(chart.GetBytes(), "image/jpeg");
		}

		//
		// GET: /Ladders/
		[OutputCache(Duration = 3600*2, VaryByCustom = GlobalConst.LobbyAccessCookieName)] // cache for 2 hours - different look for lobby and for normal
		public ActionResult Index()
		{
			var db = new ZkDataContext();

			var validAwards = db.AccountBattleAwards.Where(x => !x.SpringBattle.ResourceByMapResourceID.InternalName.Contains("SpeedMetal"));

			//var r1 = db.AccountBattleAwards.GroupBy(x=>x.AwardKey);
			var r1 = validAwards.GroupBy(x => x.AwardKey);

			var awardItems = new List<AwardItem>();
			foreach (var awardTypeInfo in r1)
			{
				var awardType = awardTypeInfo.Key;

				var monthName = DateTime.Now.ToString("MMMM");
				var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

				var topCount = validAwards.Where(x => x.AwardKey == awardType).GroupBy(x => x.Account).Max(x => x.Count());

				var resultCollectorInfo = validAwards.Where(x => x.AwardKey == awardType).GroupBy(x => x.Account).Where(x => x.Count() == topCount);

				var topCollectors = new List<Account>();
				foreach (var acct in resultCollectorInfo) topCollectors.Add(acct.Key);

				var topCountM =
					validAwards.Where(x => x.AwardKey == awardType).Where(x => x.SpringBattle.StartTime >= monthStart).GroupBy(x => x.Account).Max(x => x.Count());

				var resultCollectorInfoM =
					validAwards.Where(x => x.AwardKey == awardType).Where(x => x.SpringBattle.StartTime >= monthStart).GroupBy(x => x.Account).Where(
						x => x.Count() == topCountM);

				var topCollectorsM = new List<Account>();
				foreach (var acct in resultCollectorInfoM) topCollectorsM.Add(acct.Key);

				var resultTopScore = validAwards.Where(x => x.AwardKey == awardType);

				var topScore = 0;
				Account topAcct = null;
				var titleName = "";
				var fullTitle = "";
				SpringBattlePlayer topScoreBattlePlayer = null;
				foreach (var acct in resultTopScore)
				{
					var score = Convert.ToInt32(Regex.Replace(acct.AwardDescription, @"\D", String.Empty));
					titleName = acct.AwardDescription.Split(',').First();

					if (score > topScore)
					{
						topScore = score;
						topScoreBattlePlayer = acct.SpringBattle.SpringBattlePlayers.Single(x => x.AccountID == acct.AccountID);
						topAcct = acct.Account;
						fullTitle = string.Join(" ", acct.AwardDescription.Split(',').Skip(1));
					}
				}

				var resultTopScoreM = resultTopScore.Where(x => x.SpringBattle.StartTime >= monthStart);
				topScore = 0;
				Account topAcctM = null;
				var fullTitleM = "";
				SpringBattlePlayer topScoreBattlePlayerM = null;
				foreach (var acct in resultTopScoreM)
				{
					var score = Convert.ToInt32(Regex.Replace(acct.AwardDescription, @"\D", String.Empty));

					if (score > topScore)
					{
						topScore = score;
						topScoreBattlePlayerM = acct.SpringBattle.SpringBattlePlayers.Single(x => x.AccountID == acct.AccountID);
						topAcctM = acct.Account;
						fullTitleM = string.Join(" ", acct.AwardDescription.Split(',').Skip(1));
					}
				}

				var awardItem = new AwardItem
				                {
				                	AwardType = awardType,
				                	AwardTitle = titleName,
				                	TopScoreHolder = topAcct,
				                	TopScoreDesc = fullTitle,
				                	TopScoreBattlePlayer = topScoreBattlePlayer,
				                	TopCollectors = topCollectors,
				                	TopCollectorCount = topCount,
				                	/**/
				                	TopScoreHolderM = topAcctM,
				                	TopScoreDescM = fullTitleM,
				                	TopScoreBattlePlayerM = topScoreBattlePlayerM,
				                	TopCollectorsM = topCollectorsM,
				                	TopCollectorCountM = topCountM,
				                	/**/
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