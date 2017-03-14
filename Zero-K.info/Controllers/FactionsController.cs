﻿using System;
using System.Linq;
using System.Web.Mvc;
using LobbyClient;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class FactionsController: Controller
    {
        //
        // GET: /Factions/

        public ActionResult Index() {
            return View();
        }

        public ActionResult Detail(int id) {
            return View(new ZkDataContext().Factions.Single(x => x.FactionID == id));
        }

        [Auth]
        public ActionResult JoinFaction(int id) {
            if (Global.Account.FactionID != null) return Content("Already in faction");
            if (Global.Account.Clan != null && Global.Account.Clan.FactionID != id) return Content("Must leave current clan first");
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            acc.FactionID = id;

            Faction faction = db.Factions.Single(x => x.FactionID == id);
            if (faction.IsDeleted && !(Global.Account.Clan != null && Global.Account.Clan.FactionID == id)) throw new ApplicationException("Cannot join deleted faction");
            db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} joins {1}", acc, faction));
            db.SaveChanges();
            Global.Server.PublishAccountUpdate(acc);
            return RedirectToAction("Index", "Factions");
        }

        public static Faction PerformLeaveFaction(int accountID, bool keepClan = false, ZkDataContext db = null)
        {
            if (db == null) db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            Faction faction = acc.Faction;

            if (!keepClan && acc.Clan != null) ClansController.PerformLeaveClan(Global.AccountID);
            db.AccountRoles.DeleteAllOnSubmit(acc.AccountRolesByAccountID.Where(x => !keepClan || x.ClanID == null).ToList());
            acc.ResetQuotas();

            foreach (var ps in acc.PlanetStructures)
            {
                ps.OwnerAccountID = null;
            }

            foreach (var planet in acc.Planets)
            {
                planet.OwnerAccountID = null;
            }


            db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} leaves faction {1}", acc, acc.Faction));
            db.SaveChanges();
            PlanetWarsTurnHandler.SetPlanetOwners(new PlanetwarsEventCreator(), db);


            using (var db2 = new ZkDataContext())
            {
                Account acc2 = db2.Accounts.Single(x => x.AccountID == Global.AccountID);
                acc2.FactionID = null;
                db2.SaveChanges();

                Global.Server.PublishAccountUpdate(acc2);
                PlanetWarsTurnHandler.SetPlanetOwners(new PlanetwarsEventCreator(), db2);
            }
            return faction;
        }

        [Auth]
        public ActionResult LeaveFaction() {
            PerformLeaveFaction(Global.AccountID);
            return RedirectToAction("Index", "Factions");
        }


        public ActionResult NewTreaty(int? acceptingFactionID) {
            // first display of the form, target faction not yet set 
            var db = new ZkDataContext();
            var treaty = new FactionTreaty();
            treaty.AccountByProposingAccountID = Global.Account;
            treaty.FactionByProposingFactionID = Global.Account.Faction;
            treaty.FactionByAcceptingFactionID = db.Factions.SingleOrDefault(x => x.FactionID == acceptingFactionID);
            return View("FactionTreatyDefinition", treaty);
        }

        /// <summary>
        /// Create or modify a PlanetWars <see cref="FactionTreaty"/>
        /// </summary>
        /// <param name="factionTreatyID">Existing <see cref="FactionTreaty"/> ID, if modifying one</param>
        /// <param name="turns">How long the treaty lasts</param>
        /// <param name="acceptingFactionID"></param>
        /// <param name="effectTypeID"><see cref="TreatyEffect"/> to add or remove, if applicable</param>
        /// <param name="effectValue"></param>
        /// <param name="planetID">Specifies the <see cref="Planet"/> for planet-based effects</param>
        /// <param name="isReverse"></param>
        /// <param name="note">Diplomatic note readable by both parties, to better communicate their intentions</param>
        /// <param name="add">If not null or empty, add the specified <see cref="TreatyEffect"/></param>
        /// <param name="delete">Delete the specified <see cref="TreatyEffect"/>?</param>
        /// <param name="propose">If not null or empty, this is a newly proposed treaty</param>
        /// <returns></returns>
        public ActionResult ModifyTreaty(int factionTreatyID,
                                         int? turns,
                                         int? acceptingFactionID,
                                         int? effectTypeID,
                                         double? effectValue,
                                         int? planetID,
                                         bool? isReverse,
                                         string note,
                                         string add,int? delete,string propose) {
            if (!Global.Account.HasFactionRight(x => x.RightDiplomacy)) return Content("Not a diplomat!");

            FactionTreaty treaty;

            // submit, store treaty in db 
            var db = new ZkDataContext();

            if (factionTreatyID > 0) {
                treaty = db.FactionTreaties.Single(x => x.FactionTreatyID == factionTreatyID);
                if (treaty.TreatyState != TreatyState.Invalid) return Content("Treaty already in progress!");
            }
            else {
                treaty = new FactionTreaty();
                db.FactionTreaties.InsertOnSubmit(treaty);
                treaty.FactionByAcceptingFactionID = db.Factions.Single(x => x.FactionID == acceptingFactionID);
            }
            treaty.AccountByProposingAccountID = db.Accounts.Find(Global.AccountID);
            treaty.FactionByProposingFactionID = db.Factions.Single(x => x.FactionID == Global.FactionID);
            treaty.TurnsRemaining = turns;
            treaty.TurnsTotal = turns;
            treaty.TreatyNote = note;
            

            if (!string.IsNullOrEmpty(add)) {
                TreatyEffectType effectType = db.TreatyEffectTypes.Single(x => x.EffectTypeID == effectTypeID);
                var effect = new TreatyEffect
                             {
                                 FactionByGivingFactionID = isReverse == true ? treaty.FactionByAcceptingFactionID : treaty.FactionByProposingFactionID,
                                 FactionByReceivingFactionID = 
                                     isReverse == true ? treaty.FactionByProposingFactionID : treaty.FactionByAcceptingFactionID,
                                 TreatyEffectType = effectType,
                                 FactionTreaty = treaty
                             };
                if (effectType.HasValue) {
                    if (effectType.MinValue.HasValue && effectValue < effectType.MinValue.Value) effectValue = effectType.MinValue;
                    if (effectType.MaxValue.HasValue && effectValue > effectType.MaxValue.Value) effectValue = effectType.MaxValue;
                }
                if (effectType.HasValue) effect.Value = effectValue;
                if (effectType.IsPlanetBased) effect.PlanetID = planetID.Value;
                db.TreatyEffects.InsertOnSubmit(effect);
            }
            if (delete != null) {
                db.TreatyEffects.DeleteOnSubmit(db.TreatyEffects.Single(x=>x.TreatyEffectID == delete));
            }
            db.SaveChanges();

            if (!string.IsNullOrEmpty(propose)) {
                treaty.TreatyState = TreatyState.Proposed;
                
                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} proposes a new treaty between {1} and {2} - {3}",treaty.AccountByProposingAccountID, treaty.FactionByProposingFactionID, treaty.FactionByAcceptingFactionID, treaty));

                db.SaveChanges();
                return RedirectToAction("Detail", new { id = treaty.ProposingFactionID});
            }

            return View("FactionTreatyDefinition", treaty);
        }

     

        public ActionResult CancelTreaty(int id) {
            var db = new ZkDataContext();
            var treaty = db.FactionTreaties.Single(x => x.FactionTreatyID == id);
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            if (treaty.CanCancel(acc)) {
                treaty.TreatyState = TreatyState.Invalid;
                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("Treaty {0} between {1} and {2} cancelled by {3}", treaty, treaty.FactionByProposingFactionID, treaty.FactionByAcceptingFactionID, acc));
                db.SaveChanges();

                return RedirectToAction("Detail", new { id = Global.FactionID });
            }
            return Content("Cannot cancel");
        }

        public ActionResult CounterProposal(int id) {
            var db = new ZkDataContext();
            var treaty = db.FactionTreaties.Single(x => x.FactionTreatyID == id);
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            if (treaty.CanCancel(acc) && treaty.TreatyState == TreatyState.Proposed) {
                treaty.FactionByAcceptingFactionID = treaty.AcceptingFactionID == acc.FactionID
                                                         ? treaty.FactionByProposingFactionID
                                                         : treaty.FactionByAcceptingFactionID;
                treaty.AccountByProposingAccountID = acc;
                treaty.FactionByProposingFactionID = acc.Faction;
                treaty.TreatyState = TreatyState.Invalid;
                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} modified treaty proposal {1} between {2} and {3}", acc, treaty, treaty.FactionByProposingFactionID, treaty.FactionByAcceptingFactionID));
                db.SaveChanges();

                return View("FactionTreatyDefinition", treaty);

            }
            return Content("Not permitted");
        }

        public ActionResult TreatyDetail(int id) {
            var db = new ZkDataContext();
            return View("~/Views/Shared/DisplayTemplates/FactionTreaty.cshtml", db.FactionTreaties.Single(x => x.FactionTreatyID == id));
        }

        [Auth]
        public ActionResult AcceptTreaty(int id) {
            var db = new ZkDataContext();
            var treaty = db.FactionTreaties.Single(x => x.FactionTreatyID == id);
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            if (treaty.CanAccept(acc) && treaty.ProcessTrade(true)) {
                treaty.AcceptedAccountID = acc.AccountID;
                treaty.TreatyState = TreatyState.Accepted;
                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("Treaty {0} between {1} and {2} accepted by {3}", treaty, treaty.FactionByProposingFactionID, treaty.FactionByAcceptingFactionID, acc));
                db.SaveChanges();

                return RedirectToAction("Detail", new { id = Global.FactionID });
            }
            return Content("Cannot cancel");

        }

        /// <summary>
        /// Set faction secret topic (applied to lobby channel as well)
        /// </summary>
        public ActionResult SetTopic(int factionID, string secretTopic) {
            var db = new ZkDataContext();
            var fac = db.Factions.Single(x => x.FactionID == factionID);
            if (Global.Account.FactionID == fac.FactionID && Global.Account.HasFactionRight(x=>x.RightEditTexts)) {
                fac.SecretTopic = secretTopic;
                db.SaveChanges();
                Global.Server.SetTopic(fac.Shortcut,secretTopic, Global.Account.Name);
                return RedirectToAction("Detail", new { id = fac.FactionID });
            }
            return Content("Denied");
        }
    }
}
