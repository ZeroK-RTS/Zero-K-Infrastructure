using System;
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
            Global.Server.PublishUserProfileUpdate(acc);
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
                Global.Server.PublishUserProfileUpdate(acc2);
                PlanetWarsTurnHandler.SetPlanetOwners(new PlanetwarsEventCreator(), db2);
            }
            return faction;
        }

        [Auth]
        public ActionResult LeaveFaction() {
            PerformLeaveFaction(Global.AccountID);
            return RedirectToAction("Index", "Factions");
        }

        // first display of the form, target faction not yet set
        public ActionResult NewTreaty(int? acceptingFactionID) {
            if (Global.Account == null) // logged out while on new treaty page (or somehow got the link while not logged in)
                return Content("You must be logged in to propose treaties");
            if (!Global.Account.HasFactionRight(x => x.RightDiplomacy)) 
                return Content("Not a diplomat!");

            var db = new ZkDataContext();
            var treaty = new FactionTreaty();
            treaty.AccountByProposingAccountID = Global.Account;
            treaty.FactionByProposingFactionID = Global.Account.Faction;
            treaty.ProposingFactionID = Global.FactionID;
            
            if (treaty.ProposingFactionID == acceptingFactionID)
                return Content("Faction cannot enter into treaty with itself");
            
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
                                         TreatyUnableToTradeMode? treatyUnableToTradeMode,
                                         int? proposingFactionGuarantee,
                                         int? acceptingFactionGuarantee,
                                         string note,
                                         string add,int? delete,string propose) {
            if (Global.Account == null)
                return Content("You must be logged in to manage treaties");
            if (!Global.Account.HasFactionRight(x => x.RightDiplomacy)) return Content("Not a diplomat!");

            FactionTreaty treaty;

            // submit, store treaty in db 
            var db = new ZkDataContext();

            if (factionTreatyID > 0) {
                treaty = db.FactionTreaties.Single(x => x.FactionTreatyID == factionTreatyID);
                if (treaty.TreatyState != TreatyState.Invalid) return Content("Treaty already in progress!");
            }
            else {
                if (factionTreatyID == acceptingFactionID)
                    return Content("Faction cannot enter into treaty with itself");

                treaty = new FactionTreaty();
                db.FactionTreaties.InsertOnSubmit(treaty);
                treaty.FactionByAcceptingFactionID = db.Factions.Single(x => x.FactionID == acceptingFactionID);
            }
            treaty.AccountByProposingAccountID = db.Accounts.Find(Global.AccountID);
            treaty.FactionByProposingFactionID = db.Factions.Single(x => x.FactionID == Global.FactionID);
            treaty.TurnsRemaining = turns;
            treaty.TurnsTotal = turns;
            treaty.TreatyNote = note;
            treaty.TreatyUnableToTradeMode = treatyUnableToTradeMode ?? TreatyUnableToTradeMode.Cancel;
            treaty.ProposingFactionGuarantee = Math.Max(proposingFactionGuarantee ?? 0, 0);
            treaty.AcceptingFactionGuarantee = Math.Max(acceptingFactionGuarantee ?? 0, 0);
            

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
                if (effectType.IsPlanetBased)
                {
                    effect.PlanetID = planetID.Value;
                    effect.Planet = db.Planets.Find(planetID.Value);
                }
                if (effectType.IsPlanetBased && effect.Planet.PlanetStructures.Any(x => x.StructureType.EffectIsVictoryPlanet == true) && effectType.TreatyEffects.Any(x => x.TreatyEffectType.EffectGiveInfluence == true)) return Content("Cannot trade victory planets");
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


            return View("FactionTreatyDefinition", db.FactionTreaties.Find(treaty.FactionTreatyID));
        }

     

        public ActionResult CancelTreaty(int id) {
            var db = new ZkDataContext();
            var treaty = db.FactionTreaties.Single(x => x.FactionTreatyID == id);
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            if (treaty.CanCancel(acc)) {
                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("Treaty {0} between {1} and {2} cancelled by {3}", treaty, treaty.FactionByProposingFactionID, treaty.FactionByAcceptingFactionID, acc));

                treaty.CancelTreaty(acc.Faction);
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

                if (treaty.AcceptingFactionGuarantee == acc.FactionID)
                {
                    var pom = treaty.AcceptingFactionGuarantee;
                    treaty.AcceptingFactionGuarantee = treaty.ProposingFactionGuarantee;
                    treaty.ProposingFactionGuarantee = pom;
                }

                treaty.FactionByAcceptingFactionID = treaty.AcceptingFactionID == acc.FactionID
                                                         ? treaty.FactionByProposingFactionID
                                                         : treaty.FactionByAcceptingFactionID;
                treaty.AccountByProposingAccountID = acc;
                treaty.FactionByProposingFactionID = acc.Faction;
                treaty.TreatyState = TreatyState.Invalid;


                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} modified treaty proposal {1} between {2} and {3}", acc, treaty, treaty.FactionByProposingFactionID, treaty.FactionByAcceptingFactionID));
                db.SaveChanges();



                return View("FactionTreatyDefinition", db.FactionTreaties.Find(treaty.FactionTreatyID));

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
            if (!treaty.CanAccept(acc))
                return Content("You do not have rights to accept treaties");

            // note: we don't actually need to make sure trade can be executed before storing guarantee,
            // because if either fails we just don't save the changes to database

            var isOneTimeOnly = treaty.TreatyEffects.All(x => x.TreatyEffectType.IsOneTimeOnly);

            if (treaty.ProcessTrade(true) == null && (isOneTimeOnly || treaty.StoreGuarantee())) {
                treaty.AcceptedAccountID = acc.AccountID;
                treaty.TreatyState = TreatyState.Accepted;
                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("Treaty {0} between {1} and {2} accepted by {3}", treaty, treaty.FactionByProposingFactionID, treaty.FactionByAcceptingFactionID, acc));

                PlanetWarsTurnHandler.SetPlanetOwners(new PlanetwarsEventCreator(), db);

                db.SaveChanges();

                if (isOneTimeOnly)
                {
                    treaty.TreatyState = TreatyState.Invalid;
                    db.SaveChanges();
                }

                return RedirectToAction("Detail", new { id = Global.FactionID });
            }
            return Content("One or both parties are unable to meet treaty conditions");

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
