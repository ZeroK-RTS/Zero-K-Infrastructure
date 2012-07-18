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
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            acc.FactionID = id;

            Faction faction = db.Factions.Single(x => x.FactionID == id);
            db.Events.InsertOnSubmit(Global.CreateEvent("{0} joins {1}", acc, faction));
            db.SubmitChanges();
            return Content(string.Format("Done, welcome to the {0}!", faction.Name));
        }


        [Auth]
        public ActionResult LeaveFaction() {
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            if (acc.Clan != null) ClansController.PerformLeaveClan(Global.AccountID);
            db.AccountRoles.DeleteAllOnSubmit(acc.AccountRolesByAccountID);
            acc.ResetQuotas();

            db.Events.InsertOnSubmit(Global.CreateEvent("{0} leaves faction {1}", acc, acc.Faction));
            db.SubmitChanges();
            db.Dispose();
            db = new ZkDataContext();
            Account acc2 = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            acc2.FactionID = null;
            db.SubmitChanges();

            PlanetwarsController.SetPlanetOwners();
            return RedirectToAction("Index", "Clans");
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


        public ActionResult ModifyTreaty(int factionTreatyID,
                                         int? turns,
                                         int? acceptingFactionID,
                                         int? effectTypeID,
                                         double? effectValue,
                                         int? planetID,
                                         bool? isReverse,
                                         string add,int? delete, string propose) {
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
            treaty.AccountByProposingAccountID = Account.AccountByAccountID(db, Global.AccountID);
            treaty.FactionByProposingFactionID = db.Factions.Single(x => x.FactionID == Global.FactionID);
            treaty.TurnsRemaining = turns;
            treaty.TurnsTotal = turns;
            

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
                if (effectType.HasValue) effect.Value = effectValue;
                if (effectType.IsPlanetBased) effect.PlanetID = planetID.Value;
                db.TreatyEffects.InsertOnSubmit(effect);
            }
            if (delete != null) {
                db.TreatyEffects.DeleteOnSubmit(db.TreatyEffects.Single(x=>x.TreatyEffectID == delete));
            }
            db.SubmitAndMergeChanges();

            if (!string.IsNullOrEmpty(propose)) {
                treaty.TreatyState = TreatyState.Proposed;
                
                db.Events.InsertOnSubmit(Global.CreateEvent("{0} proposes a new treaty between {1} and {2} - {3}",treaty.AccountByProposingAccountID, treaty.FactionByProposingFactionID, treaty.FactionByAcceptingFactionID, treaty));
                var mes = string.Format("New treaty proposal between {0} and {1} offered by {2}",
                                        treaty.FactionByProposingFactionID.Name,
                                        treaty.FactionByAcceptingFactionID.Name,
                                        treaty.AccountByProposingAccountID.Name);

                SendFactionMessage(treaty.FactionByAcceptingFactionID, mes);
                SendFactionMessage(treaty.FactionByProposingFactionID, mes);

                db.SubmitAndMergeChanges();
                return RedirectToAction("Detail", new { id = treaty.ProposingFactionID});
            }

            return View("FactionTreatyDefinition", treaty);
        }

        private static void SendFactionMessage(Faction faction, string message) {
            if (Global.Nightwatch!=null && Global.Nightwatch.Tas != null) {
                Global.Nightwatch.Tas.Say(TasClient.SayPlace.Channel, faction.Shortcut, message ,true);
            }
        }

        public ActionResult CancelTreaty(int id) {
            var db = new ZkDataContext();
            var treaty = db.FactionTreaties.Single(x => x.FactionTreatyID == id);
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            if (treaty.CanCancel(acc)) {
                treaty.TreatyState = TreatyState.Invalid;
                db.Events.InsertOnSubmit(Global.CreateEvent("Treaty {0} between {1} and {2} cancelled by {3}", treaty, treaty.FactionByProposingFactionID, treaty.FactionByAcceptingFactionID, acc));
                db.SubmitAndMergeChanges();

                var mes = string.Format("Treaty between {0} and {1} cancelled by {2}",
                                        treaty.FactionByProposingFactionID.Name,
                                        treaty.FactionByAcceptingFactionID.Name,
                                        acc.Name);
                SendFactionMessage(treaty.FactionByAcceptingFactionID, mes);
                SendFactionMessage(treaty.FactionByProposingFactionID, mes);
                
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
                treaty.TreatyState = TreatyState.Invalid;
                db.SubmitAndMergeChanges();
               
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
            if (treaty.CanAccept(acc))
            {
                treaty.TreatyState = TreatyState.Accepted;
                db.Events.InsertOnSubmit(Global.CreateEvent("Treaty {0} between {1} and {2} accepted by {3}", treaty, treaty.FactionByProposingFactionID, treaty.FactionByAcceptingFactionID, acc));
                db.SubmitAndMergeChanges();

                var mes = string.Format("Treaty between {0} and {1} accepted by {2}",
                                        treaty.FactionByProposingFactionID.Name,
                                        treaty.FactionByAcceptingFactionID.Name,
                                        acc.Name);
                SendFactionMessage(treaty.FactionByAcceptingFactionID, mes);
                SendFactionMessage(treaty.FactionByProposingFactionID, mes);

                return RedirectToAction("Detail", new { id = Global.FactionID });
            }
            return Content("Cannot cancel");

        }
    }
}