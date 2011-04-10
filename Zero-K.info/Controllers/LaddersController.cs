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
				[OutputCache(Duration = 3600*2, VaryByParam = "none")]  // cache for 2 hours
        public ActionResult Index()
        {
                var db = new ZkDataContext();

                var r1 = db.AccountBattleAwards.GroupBy(x=>x.AwardKey);
                var awardItems = new List<AwardItem>();
                foreach (var awardTypeInfo in r1)
                {
                    var awardType = awardTypeInfo.Key;

                    var resultCollCount = db.AccountBattleAwards
                        .Where(x => x.AwardKey == awardType)
                        .GroupBy(x => x.Account)
                        .Max(x=>x.Count())
                        ;
                    var topCount = resultCollCount;

                    
                    var resultCollectorInfo = db.AccountBattleAwards
                        .Where(x => x.AwardKey == awardType)
                        .GroupBy(x => x.Account)
                        .Where(x => x.Count() == resultCollCount)
                        ;

                    var topCollectors = new List<Account>();
                    foreach (var acct in resultCollectorInfo)
                    {
                        topCollectors.Add(acct.Key);
                    }

                    var resultTopScore = db.AccountBattleAwards
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
                    var awardItem = new AwardItem { 
                            AwardType = awardType,
                            AwardTitle = titleName,
                            TopCollectors = topCollectors, 
                            TopCollectorCount = topCount,
                            TopScoreHolder = topAcct,
                            TopScoreBattlePlayer = topScoreBattlePlayer,
                            TopScoreDesc = fullTitle
                    };
                    awardItems.Add(awardItem);
                }

                return View("Ladders", awardItems);
        }
 
        public class AwardItem
        {
            public string AwardType;
            public string AwardTitle;
            public string TopScoreDesc;
            public List<Account> TopCollectors;
            public int TopCollectorCount;
        		public SpringBattlePlayer TopScoreBattlePlayer;
            public Account TopScoreHolder;
        }

    }
}
