using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ZkData;
using System.Text.RegularExpressions;

namespace ZeroKWeb.Controllers
{
    public class LaddersController : Controller
    {
        //
        // GET: /Ladders/
		[OutputCache(Duration = 3600*2, VaryByCustom = GlobalConst.LobbyAccessCookieName)]  // cache for 2 hours - different look for lobby and for normal
        public ActionResult Index()
        {
                var db = new ZkDataContext();

                var validAwards = db.AccountBattleAwards
                    .Where(x=> !x.SpringBattle.ResourceByMapResourceID.InternalName.Contains( "SpeedMetal" ) )
                    ;
           

                //var r1 = db.AccountBattleAwards.GroupBy(x=>x.AwardKey);
                var r1 = validAwards
                    .GroupBy(x => x.AwardKey)
                    ;

                var awardItems = new List<AwardItem>();
                foreach (var awardTypeInfo in r1)
                {
                    var awardType = awardTypeInfo.Key;

                    var monthName = DateTime.Now.ToString("MMMM");
                    var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);


                    var topCount = validAwards
                        .Where(x => x.AwardKey == awardType)
                        .GroupBy(x => x.Account)
                        .Max(x=>x.Count())
                        ;

                    var resultCollectorInfo = validAwards
                        .Where(x => x.AwardKey == awardType)
                        .GroupBy(x => x.Account)
                        .Where(x => x.Count() == topCount)
                        ;

                    var topCollectors = new List<Account>();
                    foreach (var acct in resultCollectorInfo)
                    {
                        topCollectors.Add(acct.Key);
                    }



                    var topCountM = validAwards
                        .Where(x => x.AwardKey == awardType)
                        .Where(x => x.SpringBattle.StartTime >= monthStart)
                        .GroupBy(x => x.Account)
                        .Max(x => x.Count())
                        ;

                    var resultCollectorInfoM = validAwards
                        .Where(x => x.AwardKey == awardType)
                        .Where(x => x.SpringBattle.StartTime >= monthStart)
                        .GroupBy(x => x.Account)
                        .Where(x => x.Count() == topCountM)
                        ;

                    var topCollectorsM = new List<Account>();
                    foreach (var acct in resultCollectorInfoM)
                    {
                        topCollectorsM.Add(acct.Key);
                    }




                    var resultTopScore = validAwards
                        .Where(x => x.AwardKey == awardType)
                        ;

                    var topScore = 0;
                    Account topAcct = null;
                    var titleName = "";
                    var fullTitle = "";
                	SpringBattlePlayer topScoreBattlePlayer = null;
                    foreach (var acct in resultTopScore)
                    {
                        var score = Convert.ToInt32( Regex.Replace(acct.AwardDescription, @"\D", String.Empty) );
                        titleName = acct.AwardDescription.Split(',').First();
                        
                        if( score > topScore )
                        {
                            topScore = score;
                        	topScoreBattlePlayer = acct.SpringBattle.SpringBattlePlayers.Single(x => x.AccountID == acct.AccountID);
                            topAcct = acct.Account;
                            fullTitle = string.Join(" ", acct.AwardDescription.Split(',').Skip(1));
                        }
                    }


                    var resultTopScoreM = resultTopScore
                        .Where(x => x.SpringBattle.StartTime >= monthStart)
                        ;
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



                    var awardItem = new AwardItem { 
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

                var top50Accounts = db.Accounts
                    .Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > DateTime.UtcNow.AddMonths(-1)))
                    .OrderByDescending(x => x.Elo).Take(50);

                var ladderModel = new LadderModel { AwardItems = awardItems, Top50Accounts = top50Accounts };
                return View("Ladders", ladderModel);
        }

 
        public class AwardItem
        {
            public string AwardType;
            public string AwardTitle;

            //for all time
            public Account TopScoreHolder;
            public string TopScoreDesc;
            public SpringBattlePlayer TopScoreBattlePlayer;

            public List<Account> TopCollectors;
            public int TopCollectorCount;


            //for this month
            public Account TopScoreHolderM;
            public string TopScoreDescM;
            public SpringBattlePlayer TopScoreBattlePlayerM;

            public List<Account> TopCollectorsM;
            public int TopCollectorCountM;
            
        }
        public class LadderModel
        {
            public List<AwardItem> AwardItems;
            public IQueryable<Account> Top50Accounts;
        }

    }
}
