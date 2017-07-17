using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using LobbyClient;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class LobbyController : Controller
    {
        //
        // GET: /Lobby/

        // should this use [Auth] as well?

        /// <summary>
        /// Used to start a <see cref="Mission"/>, replay or such in ZKL or Weblobby
        /// </summary>
        /// <param name="link"></param>
        [NoCache]
        public async Task<ActionResult> SendCommand(string link) {
            if (Global.Account == null) return Content("You must be logged in to the site");
            if (!Global.Server.IsLobbyConnected(Global.Account.Name)) return Content("To use this feature, run the Zero-K application and go online");
            await Global.Server.SendSiteToLobbyCommand(Global.Account.Name, new SiteToLobbyCommand() { Command = link });
            return Content("");
        }

        [NoCache]
        [Auth]
        public async Task<ActionResult> WatchBattle(int id)
        {
            Global.Server.ConnectedUsers.Get(Global.Account.Name)?.Process(new RequestConnectSpring() { BattleID = id });
            return Content("");
        }




        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult BlockedVPNs()
        {
            return View("BlockedVPNs");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult AddBlockedCompany(string companyName, string comment)
        {
            ZkDataContext db = new ZkDataContext();
            if (String.IsNullOrWhiteSpace(companyName)) return Content("Company name cannot be empty");
            db.BlockedCompanies.InsertOnSubmit(new BlockedCompany()
            {
                CompanyName = companyName,
                Comment = comment,
            });
            db.SaveChanges();

            var str = string.Format("{0} added new blocked VPN company: {1}", Global.Account.Name, companyName);
            Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, str);
            return  RedirectToAction("BlockedVPNs");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult AddBlockedHost(string hostname, string comment)
        {
            ZkDataContext db = new ZkDataContext();
            if (String.IsNullOrWhiteSpace(hostname)) return Content("Hostname cannot be empty");
            db.BlockedHosts.InsertOnSubmit(new BlockedHost()
            {
                HostName = hostname,
                Comment = comment,
            });
            db.SaveChanges();

            var str = string.Format("{0} added new blocked VPN host: {1}", Global.Account.Name, hostname);
            Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, str);
            return RedirectToAction("BlockedVPNs");
        }

        //[ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult RemoveBlockedCompany(int companyID)
        {
            ZkDataContext db = new ZkDataContext();
            BlockedCompany todel = db.BlockedCompanies.First(x => x.CompanyID == companyID);
            string name = todel.CompanyName;
            db.BlockedCompanies.DeleteOnSubmit(todel);
            db.SaveChanges();
            var str = string.Format("{0} removed blocked VPN company: {1}", Global.Account.Name, name);
            Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, str);
            return RedirectToAction("BlockedVPNs");
        }

        //[ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult RemoveBlockedHost(int hostID)
        {
            ZkDataContext db = new ZkDataContext();
            BlockedHost todel = db.BlockedHosts.First(x => x.HostID == hostID);
            string name = todel.HostName;
            db.BlockedHosts.DeleteOnSubmit(todel);
            db.SaveChanges();
            var str = string.Format("{0} removed blocked VPN host: {1}", Global.Account.Name, name);
            Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, str);
            return RedirectToAction("BlockedVPNs");
        }

        public class ChatHistoryModel
        {
            public string Channel { get; set; } = "zk";
            public SayPlace Place { get; set; } = SayPlace.Channel;
            public DateTime? TimeFrom { get; set; }
            public DateTime? TimeTo { get; set; }
            public string User { get; set; }
            public string Text { get; set; }
            public IQueryable<LobbyChatHistory> Data;
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult ChatHistory(ChatHistoryModel model) {
            model = model ?? new ChatHistoryModel();

            // do not show zkcore history for non-cores
            if (model.Channel == "zkcore" && Global.Account.DevLevel < DevLevel.RetiredCoreDeveloper) return View("LobbyChatHistory", model);


            var db = new ZkDataContext();
            var ret = db.LobbyChatHistories.Where(x=>x.SayPlace == model.Place).AsQueryable();
            if (!string.IsNullOrEmpty(model.Channel)) ret = ret.Where(x => x.Target == model.Channel);
            if (!string.IsNullOrEmpty(model.User)) ret = ret.Where(x => x.User == model.User);
            if (model.TimeFrom.HasValue) ret = ret.Where(x => x.Time >= model.TimeFrom);
            if (model.TimeTo.HasValue) ret = ret.Where(x => x.Time <= model.TimeTo);
            if (!string.IsNullOrEmpty(model.Text)) ret = ret.Where(x => x.Text.Contains(model.Text));
            
            model.Data = ret.OrderByDescending(x => x.Time);

            return View("LobbyChatHistory", model);
        }

    }
}
