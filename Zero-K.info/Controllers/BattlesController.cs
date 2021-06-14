using PlasmaShared;
using Ratings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class BattlesController: Controller
    {
        //
        // GET: /Battles/

        /// <summary>
        ///     Returns the page of the <see cref="SpringBattle" /> with the specified ID
        /// </summary>
        public ActionResult Detail(int id, bool showWinners = false) {
            var db = new ZkDataContext();
            ViewBag.ShowWinners = showWinners;
            var bat = db.SpringBattles.FirstOrDefault(x => x.SpringBattleID == id);
            if (bat == null) return Content("No such battle exists");

            if (bat.ForumThread != null)
            {
                bat.ForumThread.UpdateLastRead(Global.AccountID, false);
                db.SaveChanges();
            }

            if (Global.AccountID != 0 && !showWinners && bat.SpringBattlePlayers.Any(y => y.AccountID == Global.AccountID && !y.IsSpectator)) ViewBag.ShowWinners = true; // show winners if player played thatbattle

            return View("BattleDetail", bat);
        }

        public class BattleSearchModel
        {
            public string Title { get; set; }
            public string Map { get; set; }
            public int[] UserId { get; set; }
            public int? PlayersFrom { get; set; }
            public int? PlayersTo { get; set; }
            public AgeOption Age { get; set; }
            public YesNoAny Mission { get; set; }
            public YesNoAny Bots { get; set; }
            public YesNoAny Victory { get; set; }
            public YesNoAny Matchmaker { get; set; }
            public RatingSearchOption Rating { get; set; }
            public RankSelector Rank { get; set; } = RankSelector.Undefined;
            public int? offset { get; set; }
            public List<BattleQuickInfo> Data;
        }

        public enum YesNoAny
        {
            Any = 0,
            Yes = 1,
            No = 2
        }

        public enum AgeOption
        {
            Any = 0,
            Today = 1,
            [Description("This week")]
            ThisWeek = 2,
            [Description("This month")]
            ThisMonth = 3
        }

        /// <summary>
        ///     Returns the main battle replay list; params filter
        /// </summary>
        public ActionResult Index(BattleSearchModel model) {
            var db = new ZkDataContext();

            model = model ?? new BattleSearchModel();
            var q = db.SpringBattles.Include(x => x.SpringBattlePlayers);

            if (!string.IsNullOrEmpty(model.Title)) q = q.Where(b => b.Title.Contains(model.Title));

            if (!string.IsNullOrEmpty(model.Map)) q = q.Where(b => b.ResourceByMapResourceID.InternalName.Contains(model.Map));


            //if (user == null && Global.IsAccountAuthorized) user = Global.Account.Name;
            if (model.UserId != null) {
                int uniqueIds = model.UserId.Distinct().Count();
                switch (model.Victory)
                {
                    case YesNoAny.Any:
                        q = q.Where(b => b.SpringBattlePlayers.Where(p => model.UserId.Contains(p.AccountID) && !p.IsSpectator).Count() == uniqueIds);
                        break;
                    case YesNoAny.Yes:
                        q = q.Where(b => b.SpringBattlePlayers.Where(p => model.UserId.Contains(p.AccountID) && !p.IsSpectator && p.IsInVictoryTeam).Count() == uniqueIds);
                        break;
                    case YesNoAny.No:
                        q = q.Where(b => b.SpringBattlePlayers.Where(p => model.UserId.Contains(p.AccountID) && !p.IsSpectator && !p.IsInVictoryTeam).Count() == uniqueIds);
                        break;
                }
            }

            if (model.PlayersFrom.HasValue) q = q.Where(b => b.SpringBattlePlayers.Count(p => !p.IsSpectator) >= model.PlayersFrom);
            if (model.PlayersTo.HasValue) q = q.Where(b => b.SpringBattlePlayers.Count(p => !p.IsSpectator) <= model.PlayersTo);
            
            if (model.Age != AgeOption.Any)
            {
                var limit = DateTime.UtcNow;
                switch (model.Age)
                {
                    case AgeOption.Today:
                        limit = DateTime.Now.AddDays(-1);
                        break;
                    case AgeOption.ThisWeek:
                        limit = DateTime.UtcNow.AddDays(-7);
                        break;
                    case AgeOption.ThisMonth:
                        limit = DateTime.UtcNow.AddDays(-31);
                        break;
                }
                q = q.Where(b => b.StartTime >= limit);
            }

            if (model.Mission != YesNoAny.Any)
            {
                var bval = model.Mission == YesNoAny.Yes;
                q = q.Where(b => b.IsMission == bval);
            }

            if (model.Bots != YesNoAny.Any)
            {
                var bval = model.Bots == YesNoAny.Yes;
                q = q.Where(b => b.HasBots == bval);
            }

            if (model.Rank != RankSelector.Undefined)
            {
                int rank = (int)model.Rank;
                q = q.Where(b => b.Rank == rank);
            }

            if (model.Matchmaker != YesNoAny.Any)
            {
                var bval = model.Matchmaker == YesNoAny.Yes;
                q = q.Where(b => b.IsMatchMaker == bval);
            }

            if (model.Rating != RatingSearchOption.Any)
            {
                switch (model.Rating)
                {
                    case RatingSearchOption.Competitive:
                        q = q.Where(b => b.ApplicableRatings.HasFlag(RatingCategoryFlags.MatchMaking));
                        break;
                    case RatingSearchOption.Casual:
                        q = q.Where(b => b.ApplicableRatings.HasFlag(RatingCategoryFlags.Casual));
                        break;
                    case RatingSearchOption.Planetwars:
                        q = q.Where(b => b.ApplicableRatings.HasFlag(RatingCategoryFlags.Planetwars));
                        break;
                    case RatingSearchOption.None:
                        q = q.Where(b => b.ApplicableRatings == 0);
                        break;
                    default:
                        // The default case being no filtering should be safe enough.
                        break;
                }
            }

            q = q.OrderByDescending(b => b.StartTime);

            if (model.offset.HasValue) q = q.Skip(model.offset.Value);
            q = q.Take(Global.AjaxScrollCount);

            var result =
                q.ToList()
                    .Select(
                        b =>
                            new BattleQuickInfo
                            {
                                Battle = b,
                                Players = b.SpringBattlePlayers,
                                Map = b.ResourceByMapResourceID,
                                Mod = b.ResourceByModResourceID
                            })
                    .ToList();


            //if(result.Count == 0)
            //    return Content("");
            model.Data = result;
            if (model.offset.HasValue) return View("BattleTileList", model);
            return View("BattleIndex", model);
        }

        /// <summary>
        ///     Returns a page with the <see cref="SpringBattle" /> infolog
        /// </summary>
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult Logs(int id) {
            using (var db = new ZkDataContext())
            {
                var bat = db.SpringBattles.Single(x => x.SpringBattleID == id);
                return Content(System.IO.File.ReadAllText(string.Format(GlobalConst.InfologPathFormat, bat.EngineGameID)), "text/plain");
                    //,string.Format("infolog_{0}.txt", bat.SpringBattleID)
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult SetApplicableRatings(int BattleID, bool MatchMaking, bool Casual, bool PlanetWars)
        {
            SpringBattle battle;
            using (var db = new ZkDataContext())
            {
                battle = db.SpringBattles.Where(x => x.SpringBattleID == BattleID)
                                        .Include(x => x.ResourceByMapResourceID)
                                        .Include(x => x.SpringBattlePlayers)
                                        .Include(x => x.SpringBattleBots)
                                        .FirstOrDefault();
                if (battle.HasBots || battle.SpringBattlePlayers.Select(x => x.AllyNumber).Distinct().Count() < 2) return Content("Battle type currently not supported for ratings");
                battle.ApplicableRatings = (MatchMaking ? RatingCategoryFlags.MatchMaking : 0) | (Casual ? RatingCategoryFlags.Casual : 0) | (PlanetWars ? RatingCategoryFlags.Planetwars : 0);
                db.SaveChanges();
            }
            return Detail(BattleID);
        }
    }

    public struct BattleQuickInfo
    {
        public SpringBattle Battle { get; set; }
        public IEnumerable<SpringBattlePlayer> Players { get; set; }
        public Resource Map { get; set; }
        public Resource Mod { get; set; }
    }
}