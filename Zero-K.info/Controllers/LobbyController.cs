using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using LobbyClient;
using PlasmaShared;
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
            if (!Global.Server.IsLobbyConnected(Global.Account.Name)) return Content("To use this feature, you need to be running the game and be logged in there");
            await Global.Server.SendSiteToLobbyCommand(Global.Account.Name, new SiteToLobbyCommand() { Command = link });
            return Content("");
        }

        [NoCache]
        [Auth]
        public async Task<ActionResult> WatchPlanetBattle(int id)
        {
            var db = new ZkDataContext();
            var planet = db.Planets.Find(id);
            if (planet != null)
            {
                var battle = Global.Server.GetPlanetBattles(planet).OrderByDescending(x => x.Users.Count).FirstOrDefault();
                if (battle != null) Global.Server.ConnectedUsers.Get(Global.Account.Name)?.Process(new RequestConnectSpring() { BattleID = id });
            }

            return RedirectToAction("Planet", "Planetwars", new { id = id });
        }




        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult BlockedVPNs()
        {
            return View("BlockedVPNs");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> AddBlockedCompany(string companyName, string comment)
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
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, str);
            return  RedirectToAction("BlockedVPNs");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> AddBlockedHost(string hostname, string comment)
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
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, str);
            return RedirectToAction("BlockedVPNs");
        }

        //[ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> RemoveBlockedCompany(int companyID)
        {
            ZkDataContext db = new ZkDataContext();
            BlockedCompany todel = db.BlockedCompanies.First(x => x.CompanyID == companyID);
            string name = todel.CompanyName;
            db.BlockedCompanies.DeleteOnSubmit(todel);
            db.SaveChanges();
            var str = string.Format("{0} removed blocked VPN company: {1}", Global.Account.Name, name);
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, str);
            return RedirectToAction("BlockedVPNs");
        }

        //[ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> RemoveBlockedHost(int hostID)
        {
            ZkDataContext db = new ZkDataContext();
            BlockedHost todel = db.BlockedHosts.First(x => x.HostID == hostID);
            string name = todel.HostName;
            db.BlockedHosts.DeleteOnSubmit(todel);
            db.SaveChanges();
            var str = string.Format("{0} removed blocked VPN host: {1}", Global.Account.Name, name);
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, str);
            return RedirectToAction("BlockedVPNs");
        }

        public class ChatHistoryModel
        {
            public string Channel { get; set; } = "zk";
            public SayPlace Place { get; set; } = SayPlace.Channel;
            public DateTime? TimeFrom { get; set; }
            public DateTime? TimeTo { get; set; }
            public string User { get; set; }
            public string User2 { get; set; } // "dumb" multi-user search because the "proper" one doesnt support discord names, only ZKLS
            public string User3 { get; set; }
            public string User4 { get; set; }
            public string Text { get; set; }
            public IQueryable<LobbyChatHistory> Data;
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult ChatHistory(ChatHistoryModel model) {
            model = model ?? new ChatHistoryModel();

            // do not show zkcore history for non-cores
            if (model.Channel == "zkcore" && Global.Account.DevLevel < DevLevel.RetiredCoreDeveloper) return View("LobbyChatHistory", model);

            // do not show undelivered offline PMs to anyone
            if (model.Place == SayPlace.User) return View("LobbyChatHistory", model);

            var db = new ZkDataContext();
            var ret = db.LobbyChatHistories.Where(x=>x.SayPlace == model.Place).AsQueryable();
            if (!string.IsNullOrEmpty(model.Channel)) ret = ret.Where(x => x.Target == model.Channel);
            if (!string.IsNullOrEmpty(model.User) || !string.IsNullOrEmpty(model.User2) || !string.IsNullOrEmpty(model.User3) || !string.IsNullOrEmpty(model.User4)) ret = ret.Where(x => (x.User == model.User || x.User == model.User2 || x.User == model.User3 || x.User == model.User4));
            if (model.TimeFrom.HasValue) ret = ret.Where(x => x.Time >= model.TimeFrom);
            if (model.TimeTo.HasValue) ret = ret.Where(x => x.Time <= model.TimeTo);
            if (!string.IsNullOrEmpty(model.Text)) ret = ret.Where(x => x.Text.Contains(model.Text));
            
            model.Data = ret.OrderByDescending(x => x.Time);

            return View("LobbyChatHistory", model);
        }

        public class ChatModel
        {
            public string Channel { get; set; }
            public string User { get; set; }
            public string Message { get; set; }
            public IQueryable<LobbyChatHistory> Data = new List<LobbyChatHistory>().AsQueryable();
        }

        [Auth]
        public ActionResult ChatNotification(ChatModel model)
        {
            model = model ?? new ChatModel();

            try
            {
                using (var db = new ZkDataContext())
                {
                    db.Database.CommandTimeout = 5;
                    var acc = db.Accounts.Where(x => x.AccountID == Global.AccountID).First();
                    var ret = db.LobbyChatHistories.AsQueryable();
                    ret = ret.Where(x => x.Target == Global.Account.Name && x.SayPlace == SayPlace.User && x.Time > acc.LastChatRead);
                    if (ret.Count() != 0)
                    {
                        var ignoredIds = db.AccountRelations.Where(x => (x.Relation == Relation.Ignore) && (x.OwnerAccountID == acc.AccountID)).Select(x => x.TargetAccountID).ToList();
                        var ignoredNames = db.Accounts.Where(x => ignoredIds.Contains(x.AccountID)).Select(x => x.Name).ToHashSet();
                        model.Data = ret.OrderByDescending(x => x.Time).ToList().Where(x => !ignoredNames.Contains(x.User)).AsQueryable();
                        model.Channel = "";
                    }
                }
            } catch (Exception ex) {
                Trace.TraceError($"Error loading chat notifications for {Global.Account.Name}: {ex.Message}\n{ex.StackTrace}");
            }
            using (var db = new ZkDataContext())
            {
                var acc = db.Accounts.Where(x => x.AccountID == Global.AccountID).First();
                acc.LastChatRead = DateTime.UtcNow;
                db.SaveChanges();
            }

            return PartialView("ChatNotification", model);
        }
        [Auth]
        public async Task<ActionResult> ChatMessages(ChatModel model)
        {
            model = model ?? new ChatModel();

            var db = new ZkDataContext();
            bool isMuted = Punishment.GetActivePunishment(Global.AccountID, Request.UserHostAddress, 0, null, x => x.BanMute) != null;
            var minTime = DateTime.UtcNow.AddDays(-30);
            if (!string.IsNullOrEmpty(model.Channel))
            {
                // only show allowed channels
                if (!Global.Server.ChannelManager.CanJoin(Global.Account, model.Channel)) return PartialView("LobbyChatMessages", model);
                if (!String.IsNullOrEmpty(model.Message) && !isMuted)
                {
                    await Global.Server.GhostSay(new Say()
                    {
                        IsEmote = false,
                        Place = SayPlace.Channel,
                        Ring = false,
                        Source = SaySource.Zk,
                        Target = model.Channel,
                        Text = model.Message,
                        Time = DateTime.UtcNow,
                        User = Global.Account.Name,
                    });
                }
                string channelName = model.Channel;
                model.Data = db.LobbyChatHistories
                    .SqlQuery("SELECT TOP 30 * FROM [dbo].[LobbyChatHistories] WHERE [Target] = {0} AND [SayPlace] = {1} AND [Time] > {2} ORDER BY [Time] DESC", channelName, SayPlace.Channel, minTime)
                    .ToList().OrderBy(x => x.Time).AsQueryable();
                //Note if using Take(), it will be slow for uncommon channels like zktourney when ordering by Time and slow for common channels like zk if ordering by ID
            }
            else if (!string.IsNullOrEmpty(model.User))
            {
                if (!String.IsNullOrEmpty(model.Message) && !isMuted)
                {
                    await Global.Server.GhostSay(new Say()
                    {
                        IsEmote = false,
                        Place = SayPlace.User,
                        Ring = false,
                        Source = SaySource.Zk,
                        Target = model.User,
                        Text = model.Message,
                        Time = DateTime.UtcNow,
                        User = Global.Account.Name,
                    });
                }
                string otherName = model.User;
                string myName = Global.Account.Name;
                //Users can abuse rename to gain access to other users PMs, it's a feature
                model.Data = db.LobbyChatHistories
                    .Where(x => (x.User == otherName && x.Target == myName || x.User == myName && x.Target == otherName) && x.SayPlace == SayPlace.User && x.Time > minTime)
                    .OrderByDescending(x => x.Time).Take(30)
                    .ToList().OrderBy(x => x.Time).AsQueryable();
            }
            else
            {
                string myName = Global.Account.Name;

                var ignoredIds = db.AccountRelations.Where(x => (x.Relation == Relation.Ignore) && (x.OwnerAccountID == Global.AccountID)).Select(x => x.TargetAccountID).ToList();
                var ignoredNames = db.Accounts.Where(x => ignoredIds.Contains(x.AccountID)).Select(x => x.Name).ToHashSet();
                model.Data = db.LobbyChatHistories
                    .Where(x => x.Target == myName && x.SayPlace == SayPlace.User && x.Time > minTime)
                    .OrderByDescending(x => x.Time).Take(30)
                    .ToList()
                    .Where(x => !ignoredNames.Contains(x.User))
                    .OrderBy(x => x.Time)
                    .AsQueryable();
            }

            model.Message = "";

            return PartialView("LobbyChatMessages", model);
        }
        [Auth]
        public ActionResult Chat(ChatModel model)
        {
            model = model ?? new ChatModel();

            return View("LobbyChat", model);
        }

    }
}
