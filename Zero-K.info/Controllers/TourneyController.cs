using System;
using System.Collections.Generic;
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
            public List<TourneyBattle> Battles;
            public List<int> Team1Ids { get; set; } = new List<int>();
            public List<int> Team2Ids { get; set; } = new List<int>();
            public string Title { get; set; }
        }

        // GET: Tourney
        public ActionResult Index()
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var tourneyBattles = Global.Server.Battles.Values.Where(x => x != null).OfType<TourneyBattle>().ToList();

            return View("TourneyIndex", new TourneyModel() {Battles = tourneyBattles});
        }

        public ActionResult JoinBattle(string battleHost)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            Global.Server.ForceJoinBattle(Global.Account?.Name, battleHost);
            return RedirectToAction("Index");
        }

        public ActionResult RemoveBattle(int battleid)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var bat = Global.Server.Battles.Get(battleid);
            if (bat != null) Global.Server.RemoveBattle(bat);
            return RedirectToAction("Index");
        }

        public ActionResult RemoveMultipleBattles(double gameThreshold)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var db = new ZkDataContext();
            var tourneyBattles = Global.Server.Battles.Values.Where(x => x != null).OfType<TourneyBattle>().ToList();
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
                    Global.Server.RemoveBattle(tBat);
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult ForceJoinPlayers(int battleid)
        {
            if (!Global.IsTourneyController) return DenyAccess();
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

        public ActionResult ForceJoinMultiple()
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var db = new ZkDataContext();
            var tourneyBattles = Global.Server.Battles.Values.Where(x => x != null).OfType<TourneyBattle>().ToList();
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
                        Global.Server.ForceJoinBattle(p, tBat);
                    }
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult AddBattle(TourneyModel model)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var db = new ZkDataContext();
            {
                var tb = new TourneyBattle(Global.Server, new TourneyBattle.TourneyPrototype()
                {
                    Title = model.Title,
                    FounderName = Global.Account.Name,
                    TeamPlayers = new List<List<string>>()
                    {
                        model.Team1Ids.Select(x=> db.Accounts.Find(x)?.Name).Where(x=>x!=null).ToList(),
                        model.Team2Ids.Select(x=> db.Accounts.Find(x)?.Name).Where(x=>x!=null).ToList()
                    }
                });
                Global.Server.AddBattle(tb);
            }
            return RedirectToAction("Index");
        }

        public ActionResult AddMultipleBattles(string battleList)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var db = new ZkDataContext();

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
                    var tb = new TourneyBattle(Global.Server, new TourneyBattle.TourneyPrototype()
                    {
                        Title = bName,
                        FounderName = Global.Account.Name,
                        TeamPlayers = new List<List<string>>()
                    {
                        team1Ids.Select(x=> db.Accounts.Find(x)?.Name).Where(x=>x!=null).ToList(),
                        team2Ids.Select(x=> db.Accounts.Find(x)?.Name).Where(x=>x!=null).ToList()
                    }
                    });
                    Global.Server.AddBattle(tb);
                }
            }

            return RedirectToAction("Index");
        }

        public ActionResult ConvertFromChallonge(string challongeString, string prefix, bool randorder)
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var db = new ZkDataContext();

            List<string> formatList = new List<string>();

            string[] splitters = { "\n" };
            string[] splitString = challongeString.Split(splitters, System.StringSplitOptions.RemoveEmptyEntries);

            Random rnd = new Random();

            int tcount = 0;
            string[] teamsplit = { " ", "\t", "\n", "&" };
            string[] team = { "", "" };
            string[] teamlist = { "", "" };

            foreach(var str in splitString)
            {
                string trimstr = str.Trim();

                int i = 0;
                bool isInt = int.TryParse(trimstr, out i);

                if (!isInt)
                {
                    team[tcount] = trimstr;
                    teamlist[tcount] = string.Join(",", team[tcount].Split(teamsplit, StringSplitOptions.RemoveEmptyEntries));
                    tcount++;

                    if (tcount == 2)
                    {
                        tcount = 0;
                        int j = randorder ? rnd.Next(2) : 0;
                        formatList.Add(prefix + " " + team[j] + " vs " + team[1-j] + "," + teamlist[j] + "," + teamlist[1-j]);
                    }
                }
            }

            return Content(string.Join("//", formatList));
        }

        public ActionResult GetReplayList()
        {
            if (!Global.IsTourneyController) return DenyAccess();
            var db = new ZkDataContext();
            List<string> replayList = new List<string>();
            var tourneyBattles = Global.Server.Battles.Values.Where(x => x != null).OfType<TourneyBattle>().ToList();
            foreach (var tBat in tourneyBattles)
            {
                string line = string.Format("");
                foreach (var team in tBat.Prototype.TeamPlayers)
                {
                    foreach(var p in team)
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
                    if (bat != null) {
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
