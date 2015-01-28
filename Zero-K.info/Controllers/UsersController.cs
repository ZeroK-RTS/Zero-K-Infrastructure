using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using LobbyClient;
using NightWatch;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class UsersController: Controller
    {
        //
        // GET: /Users/
        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
        public ActionResult ChangeHideCountry(int accountID, bool hideCountry)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == accountID);

            if (hideCountry) acc.Country = "??";
            Global.Nightwatch.Tas.SetHideCountry(acc.Name, hideCountry);
            db.SubmitChanges();

            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }

        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
        public ActionResult ChangeAccountDeleted(int accountID, bool isDeleted)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == accountID);

            if (acc.IsDeleted != isDeleted)
            {
                Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format("Account {0} {1} deletion status changed by {2}", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), Global.Account.Name), true);
                Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format(" - {0} -> {1}", acc.IsDeleted, isDeleted), true);
                acc.IsDeleted = isDeleted;
            }
            db.SubmitChanges();

            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }     

        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
        public ActionResult ChangePermissions(int accountID, int adminAccountID, int springieLevel, bool zkAdmin, bool vpnException, bool isDeleted)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == accountID);
            Account adminAcc = db.Accounts.Single(x => x.AccountID == adminAccountID);
            Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format("Permissions changed for {0} {1} by {2}", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), adminAcc.Name), true);
            if (acc.SpringieLevel != springieLevel)
            {
                Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format(" - Springie rights: {0} -> {1}", acc.SpringieLevel, springieLevel), true);
                acc.SpringieLevel = springieLevel;
            }
           if (acc.IsZeroKAdmin != zkAdmin)
            {
                //reset chat priviledges to 2 if removing adminhood
                if (zkAdmin == false)
                {
                    Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format(" - Springie rights: {0} -> {1}", acc.SpringieLevel, 2), true);
                    acc.SpringieLevel = 2;
                }
                Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format(" - Admin status: {0} -> {1}", acc.IsZeroKAdmin, zkAdmin), true);
                acc.IsZeroKAdmin = zkAdmin;
                
            }
            if (acc.HasVpnException != vpnException)
            {
                Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format(" - VPN exception: {0} -> {1}", acc.HasVpnException, vpnException), true);
                acc.HasVpnException = vpnException;
            }
            db.SubmitChanges();
            Global.Nightwatch.Tas.Extensions.PublishAccountData(acc);
            
            return RedirectToAction("Detail", "Users", new { id = acc.AccountID });
        }

        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
        public ActionResult AdminUserDetail(int id)
        {
            var db = new ZkDataContext();
            var user = db.Accounts.Find(id);
            return View("AdminUserDetail", user);
        }


        public ActionResult Detail(string id)
        {
            var db = new ZkDataContext();

            int idint;
            Account user = null;
            if (int.TryParse(id, out idint)) user = db.Accounts.Find(idint);
            if (user == null) user = Account.AccountByName(db, id);
            return View("UserDetail", user);
        }


        public ActionResult Index(string name, string alias, string ip, int? userID = null)
        {
            var db = new ZkDataContext();
            IQueryable<Account> ret = db.Accounts.Where(x=> !x.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(name)) ret = ret.Where(x => x.Name.Contains(name));
            if (!string.IsNullOrEmpty(alias)) ret = ret.Where(x => x.Aliases.Contains(alias));
            if (!string.IsNullOrEmpty(ip)) ret = ret.Where(x => x.AccountIPs.Any(y => y.IP == ip));
            if (userID != null && userID != 0) ret = ret.Where(x => x.AccountUserIDs.Any(y => y.UserID == userID));

            return View("UserList", ret.Take(100));
        }

        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
        public ActionResult NewUsers(string name, string ip, int? userID = null)
        {
            var db = new ZkDataContext();
            IQueryable<Account> ret = db.Accounts.Where(x => !x.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(name)) ret = ret.Where(x => x.Name.Contains(name));
            if (!string.IsNullOrEmpty(ip)) ret = ret.Where(x => x.AccountIPs.Any(y => y.IP == ip));
            if (userID != null && userID != 0) ret = ret.Where(x => x.AccountUserIDs.Any(y => y.UserID == userID));

            return View("NewUsers", ret.OrderByDescending(x=> x.FirstLogin).Take(200));
        }

        public ActionResult LobbyDetail(string id)
        {
            var db = new ZkDataContext();
            int idint;
            Account user = null;
            if (int.TryParse(id, out idint)) user = db.Accounts.Find(idint);
            if (user == null) user = Account.AccountByName(db, id);

            return View("UserDetail", user);
        }

        const int MaxBanHours = 24 * 36525;   // 100 years

        [Auth(Role = AuthRole.ZkAdmin | AuthRole.LobbyAdmin)]
        public ActionResult Punish(int accountID,
                                   string reason,
                                   bool deleteXP,
                                   bool deleteInfluence,
                                   bool banMute,
                                   bool banCommanders,
                                   bool banSite,
                                   bool banLobby,
                                   bool banUnlocks,
                                   bool banForum,
                                   bool setRightsToZero,            
                                   string banIP,
                                   long? banUserID,
                                   double banHours)
        {
            ZkDataContext db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == accountID);

            if (banHours > MaxBanHours) banHours = MaxBanHours; // todo show some notification 

            Punishment punishment = new Punishment
                             {
                                 Time = DateTime.UtcNow,
                                 Reason = reason,
                                 BanMute = banMute,
                                 BanCommanders = banCommanders,
                                 BanSite = banSite,
                                 BanLobby = banLobby,
                                 BanExpires = DateTime.UtcNow.AddHours(banHours),
                                 BanUnlocks = banUnlocks,
                                 BanIP = banIP,
                                 BanForum = banForum,
                                 SetRightsToZero = setRightsToZero,
                                 DeleteXP = deleteXP,
                                 DeleteInfluence = deleteInfluence,
                                 CreatedAccountID = Global.AccountID,
                                 UserID = banUserID
                             };
            acc.PunishmentsByAccountID.Add(punishment);
            db.SubmitChanges();

            try
            {
                Global.Nightwatch.Tas.Extensions.PublishAccountData(acc);
                if (banLobby)
                {
                    Global.Nightwatch.Tas.AdminKickFromLobby(acc.Name, reason);
                }

                Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format("New penalty for {0} {1}  ", acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http")), true);
                Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format("Reason: {0} ", reason), true);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, ex.ToString(), false);
            }
            return RedirectToAction("Detail", new { id = accountID });
        }

        [Auth]
        public ActionResult ReportToAdmin(int id)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(id);
            return View("ReportToAdmin", acc);
        }

        public ActionResult ReportToAdminFromLobby(string id)
        {
            var db = new ZkDataContext();
            int idint;
            Account user = null;
            if (int.TryParse(id, out idint)) user = db.Accounts.Find(idint);
            if (user == null) user = Account.AccountByName(db, id);

            return View("ReportToAdmin", user);
        }

        [Auth]
        public ActionResult ReportToAdminSubmit(int accountID, string text)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(accountID);
            
            db.AbuseReports.InsertOnSubmit(new AbuseReport()
                                           {
                                               
                                               AccountID = acc.AccountID,
                                               ReporterAccountID = Global.AccountID,
                                               Time = DateTime.UtcNow,
                                               Text = text
                                           });
            db.SubmitAndMergeChanges();

            var str = string.Format("{0} {1} reports abuse by {2} {3} : {4}", Global.Account.Name, Url.Action("Detail", "Users", new { id = Global.AccountID }, "http"), acc.Name, Url.Action("Detail", "Users", new { id = acc.AccountID }, "http"), text);

            Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, str, true);
            foreach (var u in Global.Nightwatch.Tas.JoinedChannels[AuthService.ModeratorChannel].Users)
            {
                Global.Nightwatch.Tas.Ring(u);
            }

            return Content("Thank you. Your issue was reported. Moderators will now look into it.");
        }

        [Auth(Role = AuthRole.LobbyAdmin|AuthRole.ZkAdmin)]
        public ActionResult RemovePunishment(int punishmentID) {
            var db = new ZkDataContext();
            var todel = db.Punishments.First(x => x.PunishmentID == punishmentID);
            db.Punishments.DeleteOnSubmit(todel);
            db.SubmitAndMergeChanges();

            Account acc = todel.AccountByAccountID;
            string punisherName = "<unknown>";
            if (todel.CreatedAccountID != null)
            {
                Account adminAcc = db.Accounts.Find((int)todel.CreatedAccountID);
                punisherName = adminAcc.Name;
            }
            Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format("{0} removed a punishment given by {1} ", Global.Account.Name, punisherName), true);
            Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format("to {0} for: {1} ", acc.Name, todel.Reason), true);

            return RedirectToAction("Detail", "Users", new { id = todel.AccountID });
        }

        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
        public ActionResult MassBan()
        {
            return View("MassBan");
        }

        [Auth(Role = AuthRole.LobbyAdmin|AuthRole.ZkAdmin)]
        public ActionResult MassBanSubmit(string name, int startIndex, int endIndex, string reason, int banHours, bool banSite = false, bool banLobby = true, bool banIP = false, bool banID = false)
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
                    uint? userID = banID ? (uint?)acc.AccountUserIDs.OrderByDescending(x => x.LastLogin).FirstOrDefault().UserID : null;
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
                    };
                    acc.PunishmentsByAccountID.Add(punishment);

                    try
                    {
                        Global.Nightwatch.Tas.Extensions.PublishAccountData(acc);
                        if (banLobby)
                        {
                            Global.Nightwatch.Tas.AdminKickFromLobby(acc.Name, reason);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }
            }
            db.SubmitChanges();
            Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format("Mass ban executed by {4} for user series {0} ({1} - {2}): {3}",
                name, startIndex, endIndex, Url.Action("Detail", "Users", new { id = firstAccID }, "http"), Global.Account.Name), true);

            return Index(name, null, null);
        }

        public ActionResult MassBanByUserIDSubmit(int userID, double? maxAge, string reason, int banHours, bool banSite = false, bool banLobby = true, bool banIP = false, bool banID = false)
        {
            ZkDataContext db = new ZkDataContext();
            if (banHours > MaxBanHours) banHours = MaxBanHours;
            DateTime firstLoginAfter = maxAge != null? DateTime.UtcNow.AddHours(-(double)maxAge) : DateTime.MinValue; 
            foreach (Account acc in db.Accounts.Where(x => x.AccountUserIDs.Any(y => y.UserID == userID) && (maxAge == null || x.FirstLogin > firstLoginAfter) ))
            {
                uint? punishmentUserID = banID ? (uint?)acc.AccountUserIDs.OrderByDescending(x => x.LastLogin).FirstOrDefault().UserID : null;
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
                };
                acc.PunishmentsByAccountID.Add(punishment);

                try
                {
                    Global.Nightwatch.Tas.Extensions.PublishAccountData(acc);
                    if (banLobby) {
                        Global.Nightwatch.Tas.AdminKickFromLobby(acc.Name, reason);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
            db.SubmitChanges();
            Global.Nightwatch.Tas.Say(SayPlace.Channel, AuthService.ModeratorChannel, string.Format("Mass ban executed by {2} for userID {0} (max age {1})",
                userID, maxAge, Global.Account.Name), true);

            return NewUsers(null, null, userID);
        }


        /// <summary>
        /// This is a function requested by Pepe Ampere for NOTA veterans
        /// </summary>
        public ActionResult Fetch(string name, string password)
        {
            var db = new ZkDataContext();
            var acc = Account.AccountVerify(db, name, password);
            if (acc == null) return new JsonResult() {JsonRequestBehavior = JsonRequestBehavior.AllowGet};
            return new JsonResult() { Data = new
            {
                acc.AccountID,
                acc.Name,
                acc.Aliases,
                acc.FirstLogin,
                acc.LastLogin,
                acc.LobbyVersion,
                acc.Language,
                acc.Email,
                acc.Country,
                acc.EffectiveElo,
                acc.IsBot,
            }, JsonRequestBehavior = JsonRequestBehavior.AllowGet};
        }

        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult SetPassword(int accountID, string newPassword)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(accountID);
            acc.SetPasswordPlain(newPassword);
            db.SaveChanges();
            return Content(string.Format("{0} password set to {1}", acc.Name, newPassword));
        }
    }
}
