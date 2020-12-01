using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using LobbyClient;
using ZkData;
using System.Data.Entity.SqlServer;
using EntityFramework.Extensions;
using System.Data.Entity;
using Ratings;
using System.Threading.Tasks;

namespace ZeroKWeb.Controllers
{
    public class UsersController : Controller
    {
        //
        // GET: /Users/
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> ChangeHideCountry(int accountID, bool hideCountry)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.SingleOrDefault(x => x.AccountID == accountID);
            if (acc == null) return Content("Invalid accountID");

            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("{0} changed {1} hide country to {2}", Global.Account.Name, acc.Name, hideCountry));
            acc.HideCountry = hideCountry;
            // TODO reimplement ? Global.Nightwatch.Tas.SetHideCountry(acc.Name, hideCountry);
            db.SaveChanges();

            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> ChangeAccountDeleted(int accountID, bool isDeleted, string alias)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.SingleOrDefault(x => x.AccountID == accountID);
            if (acc == null) return Content("Invalid accountID");

            if (!string.IsNullOrWhiteSpace(alias))
            {
                if (!isDeleted) return Content("The Account must be deleted to allow battle relinking.");
                int aliasId;
                if (!int.TryParse(alias, out aliasId)) return Content("Not a valid number");
                Account target = db.Accounts.SingleOrDefault(x => x.AccountID == aliasId);
                if (target == null) return Content("Invalid alias accountID");
                db.SpringBattlePlayers.Where(x => x.AccountID == accountID).Update(x => new SpringBattlePlayer()
                {
                    AccountID = aliasId
                });
            }

            if (acc.IsDeleted != isDeleted)
            {
                await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Account {0} {1} deletion status changed by {2}", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), Global.Account.Name));
                await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - {0} -> {1}", acc.IsDeleted, isDeleted));
                acc.IsDeleted = isDeleted;
            }
            db.SaveChanges();

            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }

        [HttpPost]
        [Auth(Role = AdminLevel.SuperAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePermissions(int accountID, bool zkAdmin, bool tourneyController, bool vpnException)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.SingleOrDefault(x => x.AccountID == accountID);
            if (acc == null) return Content("Invalid accountID");
            Account adminAcc = Global.Account;
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Permissions changed for {0} {1} by {2}", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), adminAcc.Name));

            var curAdmin = acc.AdminLevel > AdminLevel.None;
            if (curAdmin != zkAdmin)
            {
                //reset chat priviledges to 2 if removing adminhood; remove NW subsciption to admin channel
                // FIXME needs to also terminate forbidden clan/faction subscriptions
                await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - Moderator status: {0} -> {1}", curAdmin, zkAdmin));
                acc.AdminLevel = zkAdmin ? AdminLevel.Moderator : AdminLevel.None;

            }
            if (acc.HasVpnException != vpnException)
            {
                await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - VPN exception: {0} -> {1}", acc.HasVpnException, vpnException));
                acc.HasVpnException = vpnException;
            }
            if (acc.IsTourneyController != tourneyController)
            {
                await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - Tourney Control: {0} -> {1}", acc.IsTourneyController, tourneyController));
                acc.IsTourneyController = tourneyController;
            }
            db.SaveChanges();

            await Global.Server.PublishAccountUpdate(acc);

            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> DeleteFromRatings(int accountID)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.SingleOrDefault(x => x.AccountID == accountID);
            if (acc == null) return Content("Invalid accountID");
            Account adminAcc = Global.Account;
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Ratings deleted for {0} {1} by {2}", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), adminAcc.Name));
            var battles = db.SpringBattles.Where(x => x.SpringBattlePlayers.Where(p => !p.IsSpectator).Any(p => p.AccountID == accountID))
                                    .Include(x => x.ResourceByMapResourceID)
                                    .Include(x => x.SpringBattlePlayers)
                                    .Include(x => x.SpringBattleBots);
            battles.Update(x => new SpringBattle() { ApplicableRatings = 0 });
            db.SaveChanges();

            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> UnlinkSteamID(int accountID)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.SingleOrDefault(x => x.AccountID == accountID);
            if (acc == null) return Content("Invalid accountID");
            Account adminAcc = Global.Account;
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("{2} unlinked Steam account for {0} {1} (name {3})",
                                                                                   acc.Name,
                                                                                   Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"),
                                                                                   adminAcc.Name,
                                                                                   acc.SteamName
                                                                                  ));
            acc.SteamName = null;
            acc.SteamID = null;
            db.SaveChanges();

            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult AdminUserDetail(int id)
        {
            var db = new ZkDataContext();
            var user = db.Accounts.Find(id);
            if (user == null) return Content("Invalid accountID");
            return View("AdminUserDetail", user);
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult ReportLog()
        {
            return View("ReportLog");
        }


        public ActionResult Detail(string id)
        {
            var db = new ZkDataContext();

            int idint;
            Account user = null;
            if (int.TryParse(id, out idint)) user = db.Accounts.Find(idint);
            if (user == null) user = Account.AccountByName(db, id);
            if (user == null) return Content("Invalid account (neither an ID nor name)");
            return View("UserDetail", user);
        }


        public class UsersIndexModel
        {
            public string Name { get; set; }
            public string IP { get; set; }
            public string Country { get; set; }
            public long? UserID { get; set; }
            public DateTime? RegisteredFrom { get; set; }
            public DateTime? RegisteredTo { get; set; }

            public DateTime? LastLoginFrom { get; set; }
            public DateTime? LastLoginTo { get; set; }

            public bool IsAdmin { get; set; }
            public IQueryable<Account> Data;
        }


        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult Index(UsersIndexModel model)
        {
            model = model ?? new UsersIndexModel();
            var db = new ZkDataContext();
            var ret = db.Accounts.Where(x => !x.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(model.Name))
            {
                var termLower = model.Name.ToLower();
                ret = ret.Where(x => x.Name.ToLower().Contains(termLower) || x.SteamName.Contains(model.Name));
            }
            if (Global.IsModerator)
            {
                if (!string.IsNullOrEmpty(model.IP)) ret = ret.Where(x => x.AccountIPs.Any(y => y.IP == model.IP));
                if (model.UserID.HasValue) ret = ret.Where(x => x.AccountUserIDs.Any(y => y.UserID == model.UserID));
            }
            if (!string.IsNullOrEmpty(model.Country))
            {
                var termLower = model.Country.ToLower();
                ret = ret.Where(x => x.Country.ToLower().Contains(termLower));
            }

            if (model.RegisteredFrom.HasValue) ret = ret.Where(x => x.FirstLogin >= model.RegisteredFrom);
            if (model.RegisteredTo.HasValue) ret = ret.Where(x => x.FirstLogin <= model.RegisteredTo);

            if (model.LastLoginFrom.HasValue) ret = ret.Where(x => x.LastLogin >= model.LastLoginFrom);
            if (model.LastLoginTo.HasValue) ret = ret.Where(x => x.LastLogin <= model.LastLoginTo);

            if (model.IsAdmin) ret = ret.Where(x => x.AdminLevel >= AdminLevel.Moderator);

            model.Data = ret.OrderByDescending(x => x.AccountID);

            return View("UsersIndex", model);
        }


        /// <summary>
        /// Get user detail page by username or <see cref="Account"/> ID
        /// </summary>
        /// <param name="id">Name or ID</param>
        public ActionResult LobbyDetail(string id)
        {
            var db = new ZkDataContext();
            int idint;
            Account user = null;
            if (int.TryParse(id, out idint)) user = db.Accounts.Find(idint);
            if (user == null) user = Account.AccountByName(db, id);
            if (user == null) return Content("Invalid account (neither an ID nor name)");

            return View("UserDetail", user);
        }

        const int MaxBanHours = 24 * 36525;   // 100 years

        /// <summary>
        /// Apply a <see cref="Punishment"/> (e.g. bans) and notifies lobby server
        /// </summary>
        /// <param name="accountID"><see cref="Account"/> ID of the person being punished</param>
        /// <param name="reason">Displayed reason for the penalty</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> Punish(int accountID,
                                   string reason,
                                   bool banMute,
                                   bool banVotes,
                                   bool banCommanders,
                                   bool banSite,
                                   bool banLobby,
                                   bool banSpecChat,
                                   bool banForum,
                                   bool messageOnly,
                                   string banIP,
                                   long? banUserID,
                                   string installID,
                                   double banHours)
        {
            ZkDataContext db = new ZkDataContext();
            Account acc = db.Accounts.SingleOrDefault(x => x.AccountID == accountID);
            if (acc == null) return Content("Invalid accountID");

            if (banHours > MaxBanHours) banHours = MaxBanHours; // todo show some notification 

            Punishment punishment = new Punishment
            {
                Time = DateTime.UtcNow,
                Reason = reason,
                BanMute = banMute,
                BanVotes = banVotes,
                BanCommanders = banCommanders,
                BanSite = banSite,
                BanLobby = banLobby,
                BanExpires = DateTime.UtcNow.AddHours(banHours),
                BanUnlocks = false,
                BanSpecChat = banSpecChat,
                MessageOnly = messageOnly,
                BanIP = banIP,
                BanForum = banForum,
                DeleteXP = false,
                DeleteInfluence = false,
                CreatedAccountID = Global.AccountID,
                InstallID = installID,
                UserID = banUserID
            };
            acc.PunishmentsByAccountID.Add(punishment);
            db.SaveChanges();

            // notify lobby of changes and post log message
            try
            {
                string pmAction = "";

                bool activePenalty = banLobby || banMute || banForum || banSpecChat || banVotes || banCommanders || banSite;

                string punisherName = "<unknown>";
                if (punishment.CreatedAccountID != null)
                {
                    Account adminAcc = db.Accounts.Find((int)punishment.CreatedAccountID);
                    if (adminAcc != null) punisherName = adminAcc.Name;
                }

                if (messageOnly && !activePenalty)
                {
                    await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Message sent to {0} {1} by {2} ", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), punisherName));
                    await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - message: {0} ", reason));

                    await Global.Server.GhostPm(acc.Name, string.Format("A moderator has sent you a message: {0}", reason));
                }
                else
                {

                    await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("New penalty for {0} {1} issued by {2}", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), punisherName));
                    await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - reason: {0} ", reason));
                    await Global.Server.GhostPm(acc.Name, string.Format("Your account has received moderator action, reason: {0}", reason));

                    if (banLobby == true)
                    {
                        await Global.Server.KickFromServer(Global.Account.Name, acc.Name, reason);

                        pmAction += "Lobby banned, ";
                    }
                    if (banMute == true)
                    {
                        await Global.Server.PublishAccountUpdate(acc);
                        pmAction += "Muted, ";
                    }
                    if (banForum == true)
                    {
                        pmAction += "Forum banned, ";
                    }
                    if (banSpecChat == true)
                    {
                        pmAction += "Spectator all-chat muted, ";
                    }
                    if (banVotes == true)
                    {
                        pmAction += "Vote powers restricted, ";
                    }
                    if (banCommanders == true)
                    {
                        pmAction += "Custom commanders restricted, ";
                    }
                    if (banSite == true)
                    {
                        pmAction += "Site banned, ";
                    }

                    if (activePenalty)
                    {
                        pmAction = pmAction.Substring(0, Math.Max(0, pmAction.Length - 2)); // removes trailing comma and space
                        await Global.Server.GhostPm(acc.Name, string.Format("Action taken: {0}", pmAction));
                        await Global.Server.GhostPm(acc.Name, string.Format("Total duration: {0} hours", banHours));

                        await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - duration: {0}h ", banHours));
                        await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" - penalty type: {0}", pmAction));
                    }
                    else
                    {
                        await Global.Server.GhostPm(acc.Name, "Action taken: Warning");
                        await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, " - penalty type: Warning");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, ex.ToString());
            }
            return RedirectToAction("Detail", new { id = accountID });
        }

        [Auth]
        public ActionResult ReportToAdmin(int id)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(id);
            if (acc == null) return Content("Invalid accountID");
            return View("ReportToAdmin", acc);
        }

        public ActionResult ReportToAdminFromLobby(string id)
        {
            var db = new ZkDataContext();
            int idint;
            Account user = null;
            if (int.TryParse(id, out idint)) user = db.Accounts.Find(idint);
            if (user == null) user = Account.AccountByName(db, id);
            if (user == null) return Content("Invalid account (neither an ID nor name)");

            return View("ReportToAdmin", user);
        }

        [Auth]
        [ValidateInput(false)]
        public async Task<ActionResult> ReportToAdminSubmit(int accountID, string text)
        {
            using (var db = new ZkDataContext())
            {
                var acc = db.Accounts.Find(accountID);
                if (acc == null) return Content("Invalid accountID");

                await Global.Server.ReportUser(db, Global.Account, acc, text);
            }
            return Content("Thank you. Your issue was reported. Moderators will now look into it.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> RemovePunishment(int punishmentID)
        {
            var db = new ZkDataContext();
            var todel = db.Punishments.FirstOrDefault(x => x.PunishmentID == punishmentID);
            if (todel == null) return Content("Invalid punishmentID");

            Account acc = todel.AccountByAccountID;
            string punisherName = "<unknown>";
            string reason = todel.Reason ?? "<unknown reason>";
            if (todel.CreatedAccountID != null)
            {
                Account adminAcc = db.Accounts.Find((int)todel.CreatedAccountID);
                if (adminAcc != null) punisherName = adminAcc.Name;
            }

            db.Punishments.DeleteOnSubmit(todel);
            db.SaveChanges();

            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("{0} removed a punishment given by {1} ", Global.Account.Name, punisherName));
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("to {0} for: {1} ", acc.Name, todel.Reason));

            return RedirectToAction("Detail", "Users", new { id = todel.AccountID });
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult MassBan()
        {
            return View("MassBan");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> MassBanSubmit(string name, int startIndex, int endIndex, string reason, int banHours, bool banSite = false, bool banLobby = true, bool banIP = false, bool banID = false)
        {
            ZkDataContext db = new ZkDataContext();
            int? firstAccID = null;
            if (banHours > MaxBanHours) banHours = MaxBanHours;
            for (int i = startIndex; i <= endIndex; i++)
            {
                Account acc = db.Accounts.FirstOrDefault(x => x.Name == name + i);
                if (acc != null)
                {
                    firstAccID = firstAccID ?? acc.AccountID;
                    long? userID = banID ? (uint?)acc.AccountUserIDs.OrderByDescending(x => x.LastLogin).FirstOrDefault().UserID : null;
                    string installID = banID ? acc.AccountUserIDs.OrderByDescending(x => x.LastLogin).FirstOrDefault().InstallID : null;
                    string userIP = banIP ? acc.AccountIPs.OrderByDescending(x => x.LastLogin).FirstOrDefault().IP : null;
                    System.Console.WriteLine(acc.Name, userID, userIP);
                    Punishment punishment = new Punishment
                    {
                        Time = DateTime.UtcNow,
                        Reason = reason,
                        BanSite = banSite,
                        BanLobby = banLobby,
                        BanExpires = DateTime.UtcNow.AddHours(banHours),
                        BanIP = userIP,
                        CreatedAccountID = Global.AccountID,
                        UserID = userID,
                        InstallID = installID,
                    };
                    acc.PunishmentsByAccountID.Add(punishment);

                    try
                    {
                        await Global.Server.KickFromServer(Global.Account.Name, acc.Name, reason);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }
            }
            db.SaveChanges();
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Mass ban executed by {4} for user series {0} ({1} - {2}): {3}",
                name, startIndex, endIndex, Url.Action("Detail", "Users", new { id = firstAccID }, "http"), Global.Account.Name));

            return Index(new UsersIndexModel() { Name = name });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> MassBanByUserIDSubmit(long userID, double? maxAge, string reason, int banHours, bool banSite = false, bool banLobby = true, bool banIP = false, bool banID = false)
        {
            ZkDataContext db = new ZkDataContext();
            if (banHours > MaxBanHours) banHours = MaxBanHours;
            DateTime firstLoginAfter = maxAge != null ? DateTime.UtcNow.AddHours(-(double)maxAge) : DateTime.MinValue;
            foreach (Account acc in db.Accounts.Where(x => x.AccountUserIDs.Any(y => y.UserID == userID) && (maxAge == null || x.FirstLogin > firstLoginAfter)))
            {
                long? punishmentUserID = banID ? (uint?)acc.AccountUserIDs.OrderByDescending(x => x.LastLogin).FirstOrDefault().UserID : null;
                string installID = banID ? acc.AccountUserIDs.OrderByDescending(x => x.LastLogin).FirstOrDefault().InstallID : null;
                string userIP = banIP ? acc.AccountIPs.OrderByDescending(x => x.LastLogin).FirstOrDefault().IP : null;
                System.Console.WriteLine(acc.Name, userID, userIP);
                Punishment punishment = new Punishment
                {
                    Time = DateTime.UtcNow,
                    Reason = reason,
                    BanSite = banSite,
                    BanLobby = banLobby,
                    BanExpires = DateTime.UtcNow.AddHours(banHours),
                    BanIP = userIP,
                    CreatedAccountID = Global.AccountID,
                    UserID = punishmentUserID,
                    InstallID = installID,
                };
                acc.PunishmentsByAccountID.Add(punishment);

                try
                {
                    await Global.Server.KickFromServer(Global.Account.Name, acc.Name, reason);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
            db.SaveChanges();
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Mass ban executed by {2} for userID {0} (max age {1})",
                userID, maxAge, Global.Account.Name));

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> SetPassword(int accountID, string newPassword)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(accountID);
            if (acc == null) return Content("Invalid accountID");
            if (acc.AdminLevel > AdminLevel.None) return Content("Cannot set password on this user");
            acc.SetPasswordPlain(newPassword);
            if (!string.IsNullOrEmpty(newPassword)) acc.SteamID = null;
            db.SaveChanges();
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("{0} changed {1} password", Global.Account.Name, acc.Name));
            return Content(string.Format("{0} password set to {1}", acc.Name, newPassword));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> SetUsername(int accountID, string newUsername)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(accountID);
            if (acc == null) return Content("Invalid accountID");
            if (!Account.IsValidLobbyName(newUsername)) return Content("Invalid username");
            var existing = db.Accounts.FirstOrDefault(x => x.Name.ToUpper() == newUsername.ToUpper() && x.AccountID != accountID);
            if (existing != null) return Content("Name conflict with user " + existing.AccountID);
            if (Global.Server.Battles.Any(x => x.Value.GetAllUserNames().Contains(acc.Name))) return Content(acc.Name + " is currently fighting in a battle. Rename action not advised.");
            await Global.Server.KickFromServer(Global.Account.Name, acc.Name, "Your username has been changed from " + acc.Name + " to " + newUsername + ". Please login using your new username.");

            var oldName = acc.Name;
            acc.SetName(newUsername);
            db.SaveChanges();

            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Account {0} renamed by {1}", Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), Global.Account.Name));
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format(" {0} -> {1}", oldName, newUsername));

            return Content(string.Format("{0} renamed to {1}", oldName, newUsername));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> DeleteAllForumVotes(int accountID)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.FirstOrDefault(x => x.AccountID == accountID);
            var votes = acc.AccountForumVotes;

            foreach (var vote in votes.ToList())
            {
                var post = vote.ForumPost;
                var author = post.Account;
                var oldDelta = vote.Vote;

                /*
                Console.WriteLine("Purging vote on post " + post.ForumPostID + " by author " + author.Name + ": " + oldDelta);
                Console.ReadLine();
                */

                // reverse vote effects
                if (oldDelta > 0)
                {
                    author.ForumTotalUpvotes = author.ForumTotalUpvotes - oldDelta;
                    post.Upvotes = post.Upvotes - oldDelta;
                }
                else if (oldDelta < 0)
                {
                    author.ForumTotalDownvotes = author.ForumTotalDownvotes + oldDelta;
                    post.Downvotes = post.Downvotes + oldDelta;
                }
                db.AccountForumVotes.DeleteOnSubmit(vote);
            }
            db.SaveChanges();
            await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Account {0} forum votes deleted by {1}", Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), Global.Account.Name));

            return Content(string.Format("Deleted all forum votes of {0}", acc.Name));
        }

        [HttpPost]
        [Auth]
        public ActionResult ChangePassword(string oldPassword, string newPassword, string newPassword2)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(Global.AccountID);
            if (acc == null) return Content("Invalid accountID");
            if (string.IsNullOrEmpty(acc.PasswordBcrypt)) return Content("Your account is password-less, use steam");
            if (AuthServiceClient.VerifyAccountPlain(acc.Name, oldPassword) == null)
            {
                Trace.TraceWarning("Failed password check for {0} on attempted password change", Global.Account.Name);
                Global.Server.LoginChecker.LogIpFailure(Request.UserHostAddress);
                return Content("Invalid password");
            }
            if (newPassword != newPassword2) return Content("New passwords do not match");
            if (string.IsNullOrWhiteSpace(newPassword)) return Content("New password cannot be blank");
            acc.SetPasswordPlain(newPassword);
            db.SaveChanges();
            //return Content("Old: " + oldPassword + "; new: " + newPassword);
            return RedirectToAction("Logout", "Home");
        }
    }
}
