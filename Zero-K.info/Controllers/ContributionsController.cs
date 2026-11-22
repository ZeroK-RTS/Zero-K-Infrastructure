using System;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class ContributionsController: Controller
    {
        //
        // GET: /PayPal/
        public ActionResult Index() {
            var db = new ZkDataContext();

            return View("ContributionsIndex", db.Contributions.Where(x=>x.Euros > 0).OrderByDescending(x=>x.ContributionID));
        }


        // PayPal sends IPN as a POST, and the body is the whole message. Without this a GET
        // reached the handler too - with no body, so ParseIpn read nulls - and the endpoint is
        // anonymous by necessity, so there was nothing else saying what method it took. No
        // anti-forgery token: PayPal cannot carry one, which is why verification with PayPal is
        // what this endpoint trusts, and why that now happens before anything is written.
        [HttpPost]
        [NoAntiForgeryTokenByDesign("PayPal is the caller and has no session here; the postback"
            + " to PayPal, which ImportIpnPayment makes before writing anything, is what says"
            + " the notification is genuine")]
        public ActionResult Ipn() {
            // One call, because the parsed fields and the raw bytes must describe the same request:
            // PayPal verification posts the bytes back. See ControllerCompat.ReadIpnRequest.
            byte[] raw;
            var values = this.ReadIpnRequest(out raw);
            Global.PayPalInterface.ImportIpnPayment(values, raw);
            return Content("");
        }


        [Auth]
        // The reason this is safe is not that the redeemer benefits - it is that POSSESSION OF
        // THE CODE IS THE CREDENTIAL. Anyone holding one can redeem it by visiting the link
        // themselves, so forging a visit gives an attacker nothing they could not already have,
        // and costs them the kudos. The earlier wording recorded the conclusion and invited the
        // wrong objection: that a crafted link spends somebody else's code on a stranger. It
        // does, and so does the attacker simply clicking it, which is why a token changes
        // nothing here. What would change it is a code that could be guessed; it is a Guid.
        [WritesOnGetByDesign("holding the code is the credential, so a forged visit gains an attacker nothing")]
        public ActionResult Redeem(string code) {
            var db = new ZkDataContext();
            if (string.IsNullOrEmpty(code)) return Content("Code is empty");
            var contrib = db.Contributions.SingleOrDefault(x => x.RedeemCode == code);
            if (contrib == null) return Content("No contribution with that code found");
            if (contrib.AccountByAccountID != null) return Content(string.Format("This contribution has been assigned to {0}, thank you.", contrib.AccountByAccountID.Name));
            var acc = db.Accounts.Find(Global.AccountID);
            contrib.AccountByAccountID = acc;
            acc.HasKudos = true;
            db.SaveChanges();

            return Content(string.Format("Thank you!! {0} Kudos have been added to your account {1}", contrib.KudosValue, contrib.AccountByAccountID.Name));
        }

        public ActionResult ThankYou() {
            return View("ThankYou");
        }

        /// <summary>
        /// Manually input a contribution
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult AddContribution(int accountID,int kudos, string item, string currency, double gross, double grossEur, double netEur, string email, string comment, bool isSpring, DateTime date) {
            using (var db = new ZkDataContext()) {
                var acc = db.Accounts.Find(accountID);
                var contrib = new Contribution()
                              {
                                  AccountID = accountID,
                                  ManuallyAddedAccountID = Global.AccountID,
                                  KudosValue = kudos,
                                  ItemName = item,
                                  IsSpringContribution = isSpring,
                                  Comment = comment,
                                  OriginalCurrency = currency,
                                  OriginalAmount = gross,
                                  Euros = grossEur,
                                  EurosNet = netEur,
                                  Time = date,
                                  Name = acc.Name,
                                  Email = email
                              };
                db.Contributions.InsertOnSubmit(contrib);
                db.SaveChanges();
                acc.HasKudos = true;
                db.SaveChanges();
            }


            return RedirectToAction("Index");
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult ResendEmail(int contributionID) {
            var db = new ZkDataContext();
            var contrib = db.Contributions.First(x => x.ContributionID == contributionID);
            PayPalInterface.SendEmail(contrib);
            return Content(string.Format("Email with code has been sent to {0}", contrib.Email));
        }
    }
}