using System;
using System.IO;
using System.Web.Mvc;
using PlasmaShared;

namespace ZeroKWeb.Controllers
{
    public class ContributionsController: Controller
    {
        //
        // GET: /PayPal/
        public ActionResult Index() {
            return View("ContributionsIndex");
        }


        public ActionResult Ipn(FormCollection form) {
            var path = Server.MapPath("~");

            try {
                var ipn = PayPalInterface.ParseIpn(form);
                var rawData = Request.BinaryRead(Request.ContentLength);

                System.IO.File.WriteAllText(Path.Combine(path, "pp_" + ipn.TransactionID + ".txt"),
                                            string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}",
                                                          ipn.Currency,
                                                          ipn.Email,
                                                          ipn.Gross,
                                                          ipn.ItemCode,
                                                          ipn.ItemName,
                                                          ipn.Name,
                                                          ipn.Net,
                                                          ipn.Status,
                                                          ipn.Time,
                                                          ipn.TransactionID,
                                                          PayPalInterface.VerifyRequest(rawData)));
            } catch (Exception ex) {
                System.IO.File.WriteAllText(Path.Combine(path, "pp_err.txt"), ex.ToString());
            }

            return Content("");
        }

        public ActionResult ThankYou() {
            return Content("Thank you !!");
        }
    }
}