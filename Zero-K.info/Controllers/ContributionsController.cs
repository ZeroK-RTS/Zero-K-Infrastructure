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
            Global.Nightwatch.PayPalInterface.ImportIpnPayment(form, Request.BinaryRead(Request.ContentLength));

            return Content("");
        }

        public ActionResult ThankYou() {
            return Content("Thank you !!");
        }
    }
}