using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZkData;
using ZeroKWeb.Models;
using System.Data.Linq.SqlClient;
using System.Data.Linq;

namespace ZeroKWeb.Controllers
{
    public class BattlesController : Controller
    {
        //
        // GET: /Battles/

        public ActionResult Detail(int id)
        {
          var db = new ZkDataContext();
          var bat = db.SpringBattles.Single(x => x.SpringBattleID == id);
          if (bat.ForumThread != null)
          {
            bat.ForumThread.UpdateLastRead(Global.AccountID, false);
            db.SubmitChanges();
          }
          return View(bat);
        }

        public ActionResult Index(string title,
                                  string map,
                                  string mod,
                                  string user,
                                  int? players,
                                  int? age,
                                  int? duration,
                                  bool? mission,
                                  bool? bots,
                                  int? offset) {
            var db = new ZkDataContext();
            DataLoadOptions opt = new DataLoadOptions();
            opt.LoadWith<BattleQuickInfo>(b => b.Players);
            db.LoadOptions = opt;

            IQueryable<SpringBattle> q = db.SpringBattles;

            if (!string.IsNullOrEmpty(title))
                q = q.Where(b => SqlMethods.Like(b.Title, "%" + title + "%"));

            if (!string.IsNullOrEmpty(map))
                q = q.Where(b => SqlMethods.Like(b.ResourceByMapResourceID.InternalName, "%" + map + "%"));

            if (mod == null) mod = "Zero-K";
            if (!string.IsNullOrEmpty(mod))
                q = q.Where(b => SqlMethods.Like(b.ResourceByModResourceID.InternalName, "%" + mod + "%"));

            if (user == null && Global.IsAccountAuthorized) user = Global.Account.Name;
            if (!string.IsNullOrEmpty(user)) {
                var aid = (from account in db.Accounts
                           where SqlMethods.Like(account.Name, "%" + user + "%")
                           select account.AccountID).FirstOrDefault();
                if(aid != 0)
                    q = q.Where(b => b.SpringBattlePlayers.Any(p => p.AccountID == aid));
            }

            if (players.HasValue)
                q = q.Where(b => b.SpringBattlePlayers.Where(p => !p.IsSpectator).Count() == players.Value);

            if (age.HasValue)
                switch (age) {
                    case 1:
                        q = q.Where(b => SqlMethods.DateDiffHour(b.StartTime, DateTime.UtcNow) < 24);
                        break;
                    case 2:
                        q = q.Where(b => SqlMethods.DateDiffHour(b.StartTime, DateTime.UtcNow) < 24 * 7);
                        break;
                    case 3:
                        q = q.Where(b => SqlMethods.DateDiffHour(b.StartTime, DateTime.UtcNow) < 24 * 31);
                        break;
                }

            if (duration.HasValue)
                q = q.Where(b => Math.Abs(b.Duration - duration.Value * 60) < 300);

            if (mission.HasValue)
                q = q.Where(b => b.IsMission == mission.Value);

            if (bots.HasValue)
                q = q.Where(b => b.HasBots == bots.Value);

            var q2 = q
                .OrderByDescending(b => b.StartTime)
                .Select(b =>
                    new BattleQuickInfo() {
                        Battle = b,
                        Players = b.SpringBattlePlayers,
                        Map = b.ResourceByMapResourceID,
                        Mod = b.ResourceByModResourceID
                    });

            if (offset.HasValue) q2 = q2.Skip(offset.Value);
            q2 = q2.Take(Global.AjaxScrollCount);

            var result = q2.ToList();

            //if(result.Count == 0)
            //    return Content("");
            if (offset.HasValue)
                return View("BattleTileList", result);
            else
                return View(result);
        }
    }

    public struct BattleQuickInfo {
        public SpringBattle Battle { get; set; }
        public IEnumerable<SpringBattlePlayer> Players { get; set; }
        public Resource Map { get; set; }
        public Resource Mod { get; set; }
    }
}
