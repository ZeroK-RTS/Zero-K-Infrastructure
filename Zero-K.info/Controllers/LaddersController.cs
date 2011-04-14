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

                var top50Accounts = db.Accounts.OrderByDescending(x => x.Elo).Take(50);

                var ladderModel = new LadderModel { AwardItems = awardItems, Top50Accounts = top50Accounts };
                return View("Ladders", ladderModel);
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
        public class LadderModel
        {
            public List<AwardItem> AwardItems;
            public IQueryable<Account> Top50Accounts;
        }

    }
}
