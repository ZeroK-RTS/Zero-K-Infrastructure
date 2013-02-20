using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.Controllers
{
    public class PayPalController: Controller
    {
        //
        // GET: /PayPal/

        public class IpnData
        {
            public string txn_id;
            public string receiver_email;
            public string payment_amount;
            public string payment_status;
            public string payment_currency;
            public string first_name;
            public string last_name;


        }


        /// <summary>
        /// Automagically copies values from form to field of given class
        /// </summary>
        static T DeserializeForm<T>(FormCollection form) where T:new() {
            var ret = new T();
            foreach (var field in typeof(T).GetFields()) field.SetValue(ret, form[field.Name]);
            return ret;
        }


        public ActionResult Index() {
            return View("PayPalIndex");

        }


        public ActionResult Confirm(FormCollection form) {
            var sb = new StringBuilder();
            foreach (var v in Request.QueryString.AllKeys) {
                sb.AppendFormat("{0} = {1}<br/>\n", v, Request.QueryString[v]);
            }

            foreach (var v in form.AllKeys) {
                sb.AppendFormat("{0} = {1}<br/>\n", v, form[v]);
            }

            return Content(sb.ToString());
        }


        public ActionResult Ipn(FormCollection form) {
            

            var ipn = DeserializeForm<IpnData>(form);
            //var rawData = Request.BinaryRead(Request.ContentLength);
            var sb = new StringBuilder();
            foreach (var k in form.AllKeys) {
                sb.AppendFormat("{0} = {1}\n", k, form[k]);
            }

            var path = Server.MapPath("~");
            //System.IO.File.WriteAllText(Path.Combine(path, "pp_" + ipn.txn_id + ".txt"), sb.ToString());
            System.IO.File.WriteAllText(Path.Combine(path, "pp_" +ipn.txn_id+ ".txt"), sb.ToString());


            //VerifyRequest(rawData);

            //check the payment_status is Completed
            //check that txn_id has not been previously processed
            //check that receiver_email is your Primary PayPal email
            //check that payment_amount/payment_currency are correct
            //process payment

            return Content("");
            //return Content(ipn.payment_amount);
        }


        /// <summary>
        /// Sends request back to paypal to verify its true 
        /// </summary>
        /// <returns></returns>
        bool VerifyRequest(byte[] data) {
            //Post back to either sandbox or live
            var strSandbox = "https://www.sandbox.paypal.com/cgi-bin/webscr";
            var strLive = "https://www.paypal.com/cgi-bin/webscr";

            var req = (HttpWebRequest)WebRequest.Create(strSandbox);

            //Set values for the request back
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            var strRequest = Encoding.ASCII.GetString(data);
            strRequest += "&cmd=_notify-validate";
            req.ContentLength = strRequest.Length;

            //Send the request to PayPal and get the response
            var streamOut = new StreamWriter(req.GetRequestStream(), Encoding.ASCII);
            streamOut.Write(strRequest);
            streamOut.Close();
            var streamIn = new StreamReader(req.GetResponse().GetResponseStream());
            var strResponse = streamIn.ReadToEnd();
            streamIn.Close();

            return strResponse == "VERIFIED";
        }
    }
}