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

                    var topCollectorsStr = "";
                    
                    var resultCollectorInfo = db.AccountBattleAwards
                        .Where(x => x.AwardKey == awardType)
                        .GroupBy(x => x.Account)
                        .Where(x => x.Count() == resultCollCount)
                        ;

                    var topCollectors = new List<String>();
                    foreach (var acct in resultCollectorInfo)
                    {
                        topCollectors.Add(acct.Key.Name);
                    }
                    topCollectorsStr = string.Join(", ", topCollectors);

                    var resultTopScore = db.AccountBattleAwards
                        .Where(x => x.AwardKey == awardType)
                        ;
                    
                    var topScore = 0;
                    var topAcctName = "";
                    var titleName = "";
                    var fullTitle = "";
                    foreach (var acct in resultTopScore)
                    {
                        var score = Convert.ToInt32( Regex.Replace(acct.AwardDescription, @"\D", String.Empty) );
                        titleName = acct.AwardDescription.Split(',').First();
                        
                        if( score > topScore )
                        {
                            topScore = score;
                            topAcctName = acct.Account.Name;
                            fullTitle = string.Join("", acct.AwardDescription.Split(',').Skip(1));
                        }
                    }
                    var awardItem = new AwardItem { 
                            AwardType = awardType,
                            AwardTitle = titleName,
                            TopCollectors = topCollectorsStr, 
                            TopCollectorCount = topCount,
                            TopScoreHolder = topAcctName,
                            
                            TopScoreDesc = fullTitle
                    };
                    awardItems.Add(awardItem);
                }

                return View("Ladders", awardItems);
        }
        /*
        private string FormatInt(int doubleToFormat)
        {
            System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;
            nfi.NumberGroupSeparator = " ";
            var temp = doubleToFormat.ToString("n", nfi); 
            return temp.Remove(temp.Length-3);
        }
        */
 
        public class AwardItem
        {
            public string AwardType;
            public string AwardTitle;
            public string TopScoreDesc;
            public string TopCollectors;
            public int TopCollectorCount;
            public string TopScoreHolder;
        }

    }
}
