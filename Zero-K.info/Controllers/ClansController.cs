using System;
using System.Collections.Generic;
using System.Data.Linq.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class ClansController : Controller
    {
        //
        // GET: /Clans/

        public ActionResult Index()
        {
            var db = new ZkDataContext();

            return View(db.Clans.Where(x => !x.IsDeleted));
        }

        public ActionResult ClanDiplomacy(int id)
        {
            return View("ClanDiplomacy", new ZkDataContext().Clans.Single(x => x.ClanID == id));
        }


        [Auth]
        public ActionResult Create()
        {
            if (Global.Account.Clan == null || Global.Account.HasClanRole(x=>x.RightEditTexts)) return View(Global.Clan ?? new Clan() { FactionID = Global.FactionID });
            else return Content("You already have clan and you dont have rights to it");
        }

        /// <summary>
        /// Shows clan page
        /// </summary>
        /// <returns></returns>
        public ActionResult Detail(int id)
        {
            var db = new ZkDataContext();
            var clan = db.Clans.First(x => x.ClanID == id);
            if (Global.ClanID == clan.ClanID)
            {
                if (clan.ForumThread != null)
                {
                    clan.ForumThread.UpdateLastRead(Global.AccountID, false);
                    db.SubmitChanges();
                }
            }
            return View(clan);
        }

        [Auth]
        public ActionResult ChangePlayerRights(int clanID, int accountID)
        {
            // hack rights!

            return Content("phail");
            /*var db = new ZkDataContext();
            var clan = db.Clans.Single(c => clanID == c.ClanID);
            if (!(Global.Account.HasClanRole().HasClanRights && clan.ClanID == Global.Account.ClanID || Global.Account.IsZeroKAdmin)) return Content("Unauthorized");
            var kickee = db.Accounts.Single(a => a.AccountID == accountID);
            if (kickee.IsClanFounder) return Content("Clan founders can't be modified.");
            kickee.HasClanRights = !kickee.HasClanRights;
            var ev = Global.CreateEvent("{0} {1} {2} rights to clan {3}", Global.Account, kickee.HasClanRights ? "gave" : "took", kickee, clan);
            db.Events.InsertOnSubmit(ev);
            db.SubmitChanges();
            return RedirectToAction("Detail", new { id = clanID });*/

        }


        public static Clan PerformLeaveClan(int accountID, ZkDataContext db = null) {
            if (db == null) db = new ZkDataContext();
            var clan = db.Clans.Single(x => x.ClanID == accountID);
            if (clan.Accounts.Count() > GlobalConst.ClanLeaveLimit) return null; // "This clan is too big to leave";
            var acc = db.Accounts.Single(x => x.AccountID == accountID);
            
            // remove roles
            foreach (var role in acc.AccountRolesByAccountID.Where(x => x.RoleType.IsClanOnly).ToList()) acc.AccountRolesByAccountID.Remove(role);
            
            // remove planets
            acc.Planets.Clear();
            
            foreach (var entry in acc.AccountPlanets)
            {
                entry.DropshipCount = 0;
            }
            acc.Clan = null;
            db.Events.InsertOnSubmit(Global.CreateEvent("{0} leaves clan {1}", acc, clan));
            db.SubmitChanges();
            if (!clan.Accounts.Any())
            {
                clan.IsDeleted = true;
                db.Events.InsertOnSubmit(Global.CreateEvent("{0} is disbanded", clan));
            }
            db.SubmitChanges();
            return clan;
        }


        [Auth]
        public ActionResult LeaveClan() {
            var clan = PerformLeaveClan(Global.AccountID);
            if (clan == null) return Content("This clan is too big to leave");
            PlanetwarsController.SetPlanetOwners();
            
            return RedirectToAction("Index", new { id = clan.ClanID });
        }

        [Auth]
        public ActionResult JoinClan(int id, string password)
        {
            var db = new ZkDataContext();
            var clan = db.Clans.Single(x => x.ClanID == id);
            if (clan.CanJoin(Global.Account))
            {
                if (!string.IsNullOrEmpty(clan.Password) && clan.Password != password) return View(clan.ClanID);
                else
                {
                    var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
                    acc.ClanID = clan.ClanID;
                    acc.FactionID = clan.FactionID;
                    db.Events.InsertOnSubmit(Global.CreateEvent("{0} joins clan {1}", acc, clan));
                    db.SubmitChanges();
                    return RedirectToAction("Detail", new { id = clan.ClanID });
                }
            }
            else return Content("You cannot join this clan - its full, or has password, or is different faction");
        }

        [Auth]
        public ActionResult KickPlayerFromClan(int clanID, int accountID)
        {
            var db = new ZkDataContext();
            var clan = db.Clans.Single(c => clanID == c.ClanID);
            // hack use something different than appoint roles for kicking!
            if (!(Global.Account.HasClanRole(x=>x.RightEditTexts) && clan.ClanID == Global.Account.ClanID)) return Content("Unauthorized");
            PerformLeaveClan(accountID);
            db.SubmitChanges();
            PlanetwarsController.SetPlanetOwners();
            return RedirectToAction("Detail", new { id = clanID });
        }

        [Auth]
        public ActionResult SubmitCreate(Clan clan, HttpPostedFileBase image, HttpPostedFileBase bgimage)
        {
            using (var scope = new TransactionScope())
            {
                var db = new ZkDataContext();
                var created = clan.ClanID == 0; // existing clan vs creation
                if (!created)
                {
                    if (!Global.Account.HasClanRole(x=>x.RightEditTexts) || clan.ClanID != Global.Account.ClanID) return Content("Unauthorized");
                    var orgClan = db.Clans.Single(x => x.ClanID == clan.ClanID);
                    orgClan.ClanName = clan.ClanName;
                    orgClan.LeaderTitle = clan.LeaderTitle;
                    orgClan.Shortcut = clan.Shortcut;
                    orgClan.Description = clan.Description;
                    orgClan.SecretTopic = clan.SecretTopic;
                    orgClan.Password = clan.Password;
                    // orgClan.FactionID = clan.FactionID; <- not possible to change faction
                    foreach (var a in orgClan.Accounts) a.FactionID = clan.FactionID;
                }
                else
                {
                    if (Global.Clan != null) return Content("You already have a clan");
                    if (Global.FactionID != clan.FactionID) return Content("Clan must belong to same faction");
                    db.Clans.InsertOnSubmit(clan);
                }
                if (string.IsNullOrEmpty(clan.ClanName) || string.IsNullOrEmpty(clan.Shortcut)) return Content("Name and shortcut cannot be empty!");
                if (!ZkData.Clan.IsShortcutValid(clan.Shortcut)) return Content("Shortcut must have at least 1 characters and contain only numbers and letters");
                if (db.Clans.Any(x => (SqlMethods.Like(x.Shortcut, clan.Shortcut) || SqlMethods.Like(x.ClanName, clan.ClanName)) && x.ClanID != clan.ClanID)) return Content("Clan with this shortcut or name already exists");

                if (created && (image == null || image.ContentLength == 0)) return Content("Upload image");
                if (image != null && image.ContentLength > 0)
                {
                    var im = Image.FromStream(image.InputStream);
                    if (im.Width != 64 || im.Height != 64) im = im.GetResized(64, 64, InterpolationMode.HighQualityBicubic);
                    db.SubmitChanges(); // needed to get clan id for image url - stupid way really
                    im.Save(Server.MapPath(clan.GetImageUrl()));
                }
                if (bgimage != null && bgimage.ContentLength > 0)
                {
                    var im = Image.FromStream(bgimage.InputStream);
                    db.SubmitChanges(); // needed to get clan id for image url - stupid way really
                    // DW - Actually its not stupid, its required to enforce locking.
                    // It would be possbile to enforce a pre-save id
                    im.Save(Server.MapPath(clan.GetBGImageUrl()));
                }
                db.SubmitChanges();

                if (created) // we created a new clan, set self as founder and rights
                {
                    var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
                    acc.ClanID = clan.ClanID;
                    acc.FactionID = clan.FactionID;
                    db.SubmitChanges();
                    db.Events.InsertOnSubmit(Global.CreateEvent("New clan {0} formed by {1}", clan, acc));
                    db.SubmitChanges();
                }
                scope.Complete();
            }
            return RedirectToAction("Detail", new { id = clan.ClanID });
        }

        [Auth]
        public ActionResult OfferTreaty(int targetClanID, AllyStatus ourStatus, bool ourResearch)
        {
            // hack complete
            /*
            if (!Global.Account.HasClanRights || Global.Clan == null) return Content("You don't have rights to do this");
            var db = new ZkDataContext();
            var clan = db.Clans.Single(x => x.ClanID == Global.ClanID);
            var targetClan = db.Clans.Single(x => x.ClanID == targetClanID);
            var oldEffect = clan.GetEffectiveTreaty(targetClan);
            var entry = clan.TreatyOffersByOfferingClanID.SingleOrDefault(x => x.TargetClanID == targetClanID);
            if (entry == null)
            {
                entry = new TreatyOffer() { OfferingClanID = clan.ClanID, TargetClanID = targetClanID };
                db.TreatyOffers.InsertOnSubmit(entry);
            }
            entry.AllyStatus = ourStatus;
            entry.IsResearchAgreement = ourResearch;
            var theirEntry = targetClan.TreatyOffersByOfferingClanID.SingleOrDefault(x => x.TargetClanID == clan.ClanID);
            if (theirEntry == null)
            {
                theirEntry = new TreatyOffer() { OfferingClanID = targetClanID, TargetClanID = clan.ClanID };
                db.TreatyOffers.InsertOnSubmit(theirEntry);
            }
            if (ourStatus < theirEntry.AllyStatus) theirEntry.AllyStatus = ourStatus;

            db.SubmitChanges();
            db.Events.InsertOnSubmit(Global.CreateEvent("{0} offers {1}, research: {2} to {3}", clan, ourStatus, ourResearch, targetClan));

            foreach (var acc in targetClan.Accounts)
            {
                AuthServiceClient.SendLobbyMessage(acc, string.Format("{0} wants {1}, research: {2}. Check diplomacy at: http://zero-k.info/Clan/Detail/{3}", clan.ClanName, ourStatus, ourResearch, clan.ClanID));
            }

            var newEffect = clan.GetEffectiveTreaty(targetClan);

            if (newEffect.AllyStatus != oldEffect.AllyStatus || newEffect.IsResearchAgreement != oldEffect.IsResearchAgreement)
            {
                db.Events.InsertOnSubmit(Global.CreateEvent("New effective treaty between {0} and {1}: {2}->{3}, research {4}->{5}",
                                                            clan,
                                                            targetClan,
                                                            oldEffect.AllyStatus,
                                                            newEffect.AllyStatus,
                                                            oldEffect.IsResearchAgreement,
                                                            newEffect.IsResearchAgreement));
            }
            db.SubmitChanges();

            return RedirectToAction("ClanDiplomacy", "Clans", new { id = clan.ClanID });*/
            return Content("phail");
        }

    }
}
