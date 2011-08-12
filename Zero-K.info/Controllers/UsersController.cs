using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class UsersController: Controller
    {
        //
        // GET: /Users/


        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult ChangeLobbyID(int accountID, int? newLobbyID)
        {
            var db = new ZkDataContext();
            var account = db.Accounts.Single(x => x.AccountID == accountID);
            var oldLobbyID = account.LobbyID;
            account.LobbyID = newLobbyID;
            db.SubmitChanges();
            var response = string.Format("{0} lobby ID change from {1} -> {2}", account.Name, oldLobbyID, account.LobbyID);
            foreach (var duplicate in db.Accounts.Where(x => x.LobbyID == newLobbyID && x.AccountID != accountID))
            {
                response += string.Format("\n Duplicate: {0} - {1} {2}",
                                          duplicate.Name,
                                          duplicate.AccountID,
                                          Url.Action("Detail", new { id = duplicate.AccountID }));
            }
            return Content(response);
        }

        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult AutoResolveDuplicates() {
            var db = new ZkDataContext();

            // fixes duplicate name by preserving last working lobbyID 
            foreach (var dupl in db.Accounts.Where(x=>x.LobbyID != null).GroupBy(x => x.Name).Where(x => x.Count() > 1))
            {
                var dupAccounts = db.Accounts.Where(x => x.Name == dupl.Key).ToList();
                var bestAccount = dupAccounts.OrderByDescending(x => x.LastLogin).First();
                foreach (var ac in dupAccounts)
                {
                    if (ac.LobbyID != bestAccount.LobbyID) ac.LobbyID = null;
                }
            }
            db.SubmitChanges();

            // fixes duplicate lobbyID by preserving newer account
            foreach (var dupl in db.Accounts.GroupBy(x => x.LobbyID).Where(x => x.Count() > 1 && x.Key != null)) {
                var dupAccounts = db.Accounts.Where(x => x.LobbyID == dupl.Key).ToList();
                var bestAccount = dupAccounts.OrderByDescending(x => x.Level).First();
                foreach (var ac in dupAccounts) {
                    if (ac.AccountID != bestAccount.AccountID) ac.LobbyID = null;
                }
            }
            db.SubmitChanges();


            return Redirect("Duplicates");
        }



        public ActionResult Duplicates() {
            IEnumerable<Account> ret;

            var db = new ZkDataContext();
            ret = db.ExecuteQuery<Account>("select  * from account where lobbyid in (select lobbyid from (select lobbyid, count(*)  as cnt from account group by (lobbyid)) as lc where cnt > 1) and LobbyID is not null order by lobbyid");
            ret = ret.Union(db.ExecuteQuery<Account>("select * from account where name in (select name from (select name, count(*)  as cnt from account where lobbyid is not null group by (name)) as lc where cnt > 1) order by name"));
            return View(ret);
        }

        public ActionResult Detail(int id)
        {
            var db = new ZkDataContext();
            return View("UserDetail", db.Accounts.FirstOrDefault(x => x.AccountID == id));
        }

        public ActionResult Index(string name, string alias)
        {
            var db = new ZkDataContext();
            var ret = db.Accounts.AsQueryable();

            if (!string.IsNullOrEmpty(name)) ret = ret.Where(x => x.Name.Contains(name));
            if (!string.IsNullOrEmpty(alias)) ret = ret.Where(x => x.Aliases.Contains(alias));

            return View("UserList", ret.Take(100));
        }

        public ActionResult LobbyDetail(int id)
        {
            var db = new ZkDataContext();
            return View("UserDetail", db.Accounts.FirstOrDefault(x => x.LobbyID == id));
        }


        public ActionResult Punish(int accountID,
                                   string reason,
                                   bool deleteXP,
                                   bool deleteInfluence,
                                   bool banMute,
                                   bool banCommanders,
                                   bool banSite,
                                   bool banLobby,
                                   bool banUnlocks,
                                   DateTime? banExpires)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Single(x => x.AccountID == accountID);
            var punishment = new Punishment()
                                {
                                    Time = DateTime.UtcNow,
                                    Reason = reason,
                                    BanMute = banMute,
                                    BanCommanders = banCommanders,
                                    BanSite = banSite,
                                    BanLobby = banLobby,
                                    BanExpires = banExpires,
                                    BanUnlocks = banUnlocks
                                };
            acc.Punishments.Add(punishment);

            var str = new StringBuilder();
            if (deleteInfluence)
            {
                var influence = db.AccountPlanets.Where(x => x.AccountID == accountID).Sum(x => (int?)x.Influence);

                str.AppendFormat("Removed {0} planetwars Influence\n", influence);
                foreach (var entry in db.AccountPlanets.Where(x => x.AccountID == accountID)) entry.Influence = 0;
            }

            if (deleteXP)
            {
                str.AppendFormat("Removed {0} XP\n", acc.XP);
                acc.Commanders.Clear();
                acc.AccountUnlocks.Clear();
                acc.XP = 0;
            }
            if (banCommanders) str.AppendFormat("Banned from using customized commanders\n");
            if (banUnlocks) str.AppendFormat("Banned from using unlocks\n");
            if (banExpires.HasValue) str.AppendFormat("Ban expires on {0} GMT\n", banExpires);
            punishment.Punishment1 = str.ToString();

            db.SubmitChanges();
            return RedirectToAction("Detail", new { id = accountID });
        }
    }
}