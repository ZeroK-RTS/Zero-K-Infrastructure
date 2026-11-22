using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PlasmaShared;
using ZkData;
using ZkLobbyServer;

namespace ZeroKWeb.Controllers
{
    public class TourneyController : Controller
    {
        public class TourneyModel
        {
            public List<TourneyBattleInfo> Battles;
            public List<int> Team1Ids { get; set; } = new List<int>();
            public List<int> Team2Ids { get; set; } = new List<int>();
            public string Title { get; set; }

            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string ModoptString { get; set; } = "";
        }

        // GET: Tourney
        public ActionResult Index()
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var tourneyBattles = Global.LobbyApi.GetTourneyBattles();

            return View("TourneyIndex", new TourneyModel() { Battles = tourneyBattles });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult JoinBattle(string battleHost)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            Global.LobbyApi.ForceJoinBattle(Global.Account?.Name, battleHost);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveBattle(int battleid)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            Global.LobbyApi.RemoveTourneyBattle(battleid);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveMultipleBattles(double gameThreshold)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var db = new ZkDataContext();
            var tourneyBattles = Global.LobbyApi.GetTourneyBattles();
            foreach (var tBat in tourneyBattles)
            {
                int batCount = 0;
                foreach (var deb in tBat.Debriefings)
                {
                    var bat = db.SpringBattles.FirstOrDefault(x => x.SpringBattleID == deb.ServerBattleID);
                    if (bat != null) { batCount++; }
                }
                if (batCount >= gameThreshold)
                {
                    // delete this room, all the required games have been played
                    Global.LobbyApi.RemoveTourneyBattle(tBat.BattleID);
                }
            }
            return RedirectToAction("Index");
        }

        // Moves every player in a tournament battle into it. Gated on IsTourneyController,
        // which is who the forgery would need to reach - the same shape as the moderator-only
        // GETs closed in #281 and #282.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForceJoinPlayers(int battleid)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var bat = Global.LobbyApi.GetTourneyBattle(battleid);
            if (bat != null)
            {
                foreach (var p in bat.Prototype.TeamPlayers.SelectMany(x => x))
                {
                    Global.LobbyApi.ForceJoinTourneyBattle(p, bat.BattleID);
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForceJoinMultiple()
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var db = new ZkDataContext();
            var tourneyBattles = Global.LobbyApi.GetTourneyBattles();
            foreach (var tBat in tourneyBattles)
            {
                int batCount = 0;
                foreach (var deb in tBat.Debriefings)
                {
                    var bat = db.SpringBattles.FirstOrDefault(x => x.SpringBattleID == deb.ServerBattleID);
                    if (bat != null) { batCount++; }
                }
                if (batCount == 0)
                {
                    // games unplayed here, force players into room
                    foreach (var p in tBat.Prototype.TeamPlayers.SelectMany(x => x))
                    {
                        Global.LobbyApi.ForceJoinTourneyBattle(p, tBat.BattleID);
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBattle(TourneyModel model)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var db = new ZkDataContext();
            {
                var tb = new TourneyPrototypeInfo()
                {
                    Title = model.Title,
                    FounderName = Global.Account.Name,
                    TeamPlayers = new List<List<string>>()
                    {
                        model.Team1Ids.Select(x=> db.Accounts.Find(x)?.Name).Where(x=>x!=null).ToList(),
                        model.Team2Ids.Select(x=> db.Accounts.Find(x)?.Name).Where(x=>x!=null).ToList()
                    }
                };

                string[] optionSplitters = { "," };
                string[] modoptsStrings = model.ModoptString.Split(optionSplitters, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var modStr in modoptsStrings)
                {
                    bool validSetting = true;
                    string[] modComponent = modStr.Split('=');
                    if (modComponent.Length == 2)
                    {
                        tb.ModOptions.Add(modComponent[0], modComponent[1]);
                    }
                }
                Global.LobbyApi.CreateTourneyBattle(tb);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddMultipleBattles(string battleList, string modoptStringMult)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var db = new ZkDataContext();

            // parse modoptions
            Dictionary<string, string> modopts = new Dictionary<string, string>();
            string[] optionSplitters = { "," };
            string[] modoptsStrings = modoptStringMult.Split(optionSplitters, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var modStr in modoptsStrings)
            {
                bool validSetting = true;
                string[] modComponent = modStr.Split('=');
                if (modComponent.Length == 2)
                {
                    modopts.Add(modComponent[0], modComponent[1]);
                }
            }

            // parse and creatge battles
            string[] splitters = { "//" };
            string[] battleSpecs = battleList.Split(splitters, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (var bSpec in battleSpecs)
            {
                bool validBattle = true;
                string[] bItems = bSpec.Split(',');
                string bName = bItems[0];
                // must have an even number of players; the first N/2 are assigned to the first team
                if (bItems.Length % 2 == 0) { validBattle = false; };
                int playersPerTeam = (bItems.Length - 1) / 2;
                List<int> team1Ids = new List<int>();
                List<int> team2Ids = new List<int>();
                // if any of the remaining entries are not ints or account names the battle is invalid
                for (int i = 1; i <= playersPerTeam; i++)
                {
                    Account tryAcc = Account.AccountByName(db, bItems[i]);
                    if (tryAcc != null)
                    {
                        team1Ids.Add(tryAcc.AccountID);
                    }
                    else
                    {
                        try { team1Ids.Add(Int32.Parse(bItems[i])); }
                        catch (FormatException) { validBattle = false; }
                    }
                }
                for (int i = playersPerTeam + 1; i < bItems.Length; i++)
                {
                    Account tryAcc = Account.AccountByName(db, bItems[i]);
                    if (tryAcc != null)
                    {
                        team2Ids.Add(tryAcc.AccountID);
                    }
                    else
                    {
                        try { team2Ids.Add(Int32.Parse(bItems[i])); }
                        catch (FormatException) { validBattle = false; }
                    }
                }

                if (validBattle)
                {
                    var tb = new TourneyPrototypeInfo()
                    {
                        Title = bName,
                        FounderName = Global.Account.Name,
                        TeamPlayers = new List<List<string>>()
                    {
                        team1Ids.Select(x=> db.Accounts.Find(x)?.Name).Where(x=>x!=null).ToList(),
                        team2Ids.Select(x=> db.Accounts.Find(x)?.Name).Where(x=>x!=null).ToList()
                    }
                    };

                    foreach (KeyValuePair<string, string> kvp in modopts)
                    {
                        tb.ModOptions.Add(kvp.Key, kvp.Value);
                    }

                    Global.LobbyApi.CreateTourneyBattle(tb);
                }
            }

            return RedirectToAction("Index");
        }

        public ActionResult GetReplayList()
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var db = new ZkDataContext();
            List<string> replayList = new List<string>();
            var tourneyBattles = Global.LobbyApi.GetTourneyBattles();
            foreach (var tBat in tourneyBattles)
            {
                string line = string.Format("");
                foreach (var team in tBat.Prototype.TeamPlayers)
                {
                    foreach (var p in team)
                    {
                        line += "@U" + Account.AccountByName(db, p).AccountID + ", ";
                    }
                    line = line.Remove(line.Length - 2);
                    line += " vs ";
                }
                line = line.Remove(line.Length - 4);
                line += ": ";

                int batCount = 0;
                foreach (var deb in tBat.Debriefings)
                {
                    var bat = db.SpringBattles.FirstOrDefault(x => x.SpringBattleID == deb.ServerBattleID);
                    if (bat != null)
                    {
                        batCount++;
                        line += "@B" + deb.ServerBattleID + ", ";
                    }

                }
                line = line.Remove(line.Length - 2);
                if (batCount > 0)
                {
                    replayList.Add(line);
                }
            }
            return Content(string.Join("<br />", replayList));
        }

        private ActionResult DenyAccess()
        {
            return Content("You don't have access to TourneyControl");
        }
    }
}