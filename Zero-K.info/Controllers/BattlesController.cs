using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZkData;
using ZeroKWeb.Models;
using System.Data.Linq.SqlClient;

namespace ZeroKWeb.Controllers
{
    public class BattlesController : Controller
    {
        //
        // GET: /Battles/

        public ActionResult Index()
        {
            return View();
        }

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

        public ActionResult Search(string title,
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

            IQueryable<SpringBattle> q = db.SpringBattles;

            if (!string.IsNullOrEmpty(title))
                q = q.Where(b => SqlMethods.Like(b.Title, "%" + title + "%"));
            if (!string.IsNullOrEmpty(map))
                q = q.Where(b => SqlMethods.Like(b.ResourceByMapResourceID.InternalName, "%" + map + "%"));
            if (mod == null) mod = "Zero-K";
            if (!string.IsNullOrEmpty(mod))
                q = q.Where(b => SqlMethods.Like(b.ResourceByModResourceID.InternalName, "%" + mod + "%"));
            //if (user == null && Global.IsAccountAuthorized) user = Global.Account.Name;
            if (!string.IsNullOrEmpty(user))
                q = q.Where(b => b.SpringBattlePlayers.Any(p => SqlMethods.Like(p.Account.Name, "%" + user + "%")));
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

            q = q.OrderByDescending(b => b.StartTime);

            if (offset.HasValue) q = q.Skip(offset.Value);
            q = q.Take(Global.AjaxScrollCount);

            if (offset.HasValue)
                return View("BattleTileList", q);
            else
                return View(q);
        }
    }
}
