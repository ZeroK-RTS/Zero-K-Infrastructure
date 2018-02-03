using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZkData;
using ZkLobbyServer;

namespace ZeroKWeb.Controllers
{
    public class TourneyController : Controller
    {
        // GET: Tourney
        public ActionResult Index()
        {
            var tourneyBattles = Global.Server.Battles.Values.Where(x => x != null).OfType<TourneyBattle>().ToList();

            return View("TourneyIndex", tourneyBattles);
        }

        public ActionResult JoinBattle(string battleHost)
        {
            Global.Server.ForceJoinBattle(Global.Account?.Name, battleHost);
            return RedirectToAction("Index");
        }

        public ActionResult RemoveBattle(int battleid)
        {
            var bat = Global.Server.Battles.Get(battleid);
            if (bat != null) Global.Server.RemoveBattle(bat);
            return RedirectToAction("Index");
        }

        public ActionResult ForceJoinPlayers(int battleid)
        {
            var bat = Global.Server.Battles.Get(battleid) as TourneyBattle;
            if (bat != null)
            {
                foreach (var p in bat.Prototype.TeamPlayers.SelectMany(x => x))
                {
                    Global.Server.ForceJoinBattle(p, bat);
                }
            }
            return RedirectToAction("Index");
        }
    }
}