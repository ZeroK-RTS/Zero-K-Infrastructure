using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using ZkData;

namespace ZkData
{
    public class PayPalInterface
    {
        const string SpringItemCode = "SpringRTS";
        const double conversionMultiplier = 0.98; // extra cost of conversion from foreign currency

        public event Action<string> Error = (e) => { };
        public event Action<Contribution> NewContribution = (c) => { };


        public static DateTime ConvertPayPalDateTime(string payPalDateTime) {
            // accept a few different date formats because of PST/PDT timezone and slight month difference in sandbox vs. prod.  
            string[] dateFormats =
            {
                "HH:mm:ss MMM dd, yyyy PST", "HH:mm:ss MMM. dd, yyyy PST", "HH:mm:ss MMM dd, yyyy PDT",
                "HH:mm:ss MMM. dd, yyyy PDT"
            };
            DateTime outputDateTime;

            DateTime.TryParseExact(payPalDateTime, dateFormats, new CultureInfo("en-US"), DateTimeStyles.None, out outputDateTime);

            // convert to local timezone  
            outputDateTime = outputDateTime.AddHours(8);

            return outputDateTime;
        }

        public static double ConvertToEuros(string fromCurrency, double fromAmount) {
            using (var wc = new WebClient()) {
                var response =
                    wc.DownloadString(string.Format("http://rate-exchange.appspot.com/currency?from={0}&to=EUR&q={1}", fromCurrency, fromAmount));
                var ret = JsonConvert.DeserializeObject<ConvertResponse>(response);
                return ret.v*conversionMultiplier;
            }
        }

        public static string GetItemCode(int? accountID, int? jarID) {
            return string.Format("ZK_ID_{0}_JAR_{1}", accountID, jarID);
        }

        /// <summary>
        /// How a notification is checked with PayPal. A field so that a test can answer without a
        /// network: nothing else replaces it, and the default is the real postback.
        /// </summary>
        public Func<byte[], bool> Verifier = VerifyRequest;

        /// <summary>
        /// Takes a PayPal IPN notification and records the payment it describes.
        ///
        /// **PayPal is asked whether the notification is genuine BEFORE anything is written.**
        /// It used to be asked after: AddPayPalContribution ran first and verification was a
        /// postscript, so one unauthenticated request to /Contributions/Ipn - the endpoint is
        /// anonymous, as it must be - already had its full effect by the time PayPal said the
        /// notification was invalid. The effects were not nominal. The sender chooses every
        /// field, so a forged notification inserted a Contribution of any size under any name
        /// onto the public /Contributions page, set HasKudos on whichever account the item code
        /// named (ZK_ID_{accountID}_JAR_{jarID} is attacker-supplied text), mailed a working
        /// redeem code from the team address to an address of the sender's choosing, and
        /// announced "WOOHOO! New contribution" to the public zk lobby channel. The remedy was a
        /// comment on the row and a message to zkdev; the grants stood.
        ///
        /// Three outcomes, because "could not ask" is not "the answer was no":
        ///
        /// - INVALID - PayPal says it did not send this. Nothing is written. The parsed fields go
        ///   into the alert so that a genuine payment misread this way is still recoverable by
        ///   hand. That matters more than it looks: VerifyRequest posts the bytes back as ASCII,
        ///   so a donor whose name is not ASCII may fail a verification they should pass.
        /// - could not reach PayPal - recorded and flagged, which is what happened before this
        ///   change anyway (VerifyRequest threw, the outer catch logged it, and the row written a
        ///   moment earlier stayed). A network fault must not lose a real donation.
        /// - VERIFIED - recorded, and the grants happen.
        /// </summary>
        public void ImportIpnPayment(NameValueCollection values, byte[] rawRequest) {
            try {
                var parsed = ParseIpn(values);

                bool? verified;
                try {
                    verified = Verifier(rawRequest);
                } catch (Exception ex) {
                    // Asking failed, which says nothing about the notification.
                    Trace.TraceError("PayPal verification could not be performed: {0}", ex);
                    verified = null;
                }

                if (verified == false) {
                    Error(
                        string.Format(
                            "Rejected an IPN that PayPal says it did not send - NOTHING was recorded. " +
                            "If this was a real payment it must be entered by hand. " +
                            "txn {0}, name {1}, email {2}, {3} {4}, item {5}",
                            parsed.TransactionID,
                            parsed.Name,
                            parsed.Email,
                            parsed.Gross,
                            parsed.Currency,
                            parsed.ItemCode));
                    return;
                }

                var contribution = AddPayPalContribution(parsed, granted: verified == true);
                if (contribution != null && verified == null) {
                    Error(
                        string.Format(
                            "Warning, transaction {0} by {1} COULD NOT BE VERIFIED with PayPal, check that it is not a fake! {2}/Contributions ",
                            parsed.TransactionID,
                            parsed.Name,
                            GlobalConst.BaseSiteUrl));
                    using (var db = new ZkDataContext()) {
                        db.Contributions.First(x => x.ContributionID == contribution.ContributionID).Comment = "VERIFICATION UNAVAILABLE";
                        db.SaveChanges();
                    }
                }
            } catch (Exception ex) {
                Trace.TraceError(ex.ToString());
                Error(ex.ToString());
            }
        }

        public void ImportPaypalHistory(string folder) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            double sum = 0;
            foreach (var file in Directory.GetFiles(folder, "*.csv")) foreach (var info in ParseCsvLog(File.OpenRead(file))) AddPayPalContribution(info);
        }


        public static IEnumerable<ParsedData> ParseCsvLog(Stream file) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var csv = new CsvTable(file, true, true, ',', "windows-1252");
            return
                csv.Select(
                    row =>
                    new ParsedData()
                    {
                        Time = DateTime.Parse(row["Date"] + " " + row["Time"]),
                        Name = row["Name"],
                        Status = row["Status"],
                        Currency = row["Currency"],
                        Gross = double.Parse(row["Gross"]),
                        Net = double.Parse(row["Net"]),
                        Email = row["From Email Address"],
                        TransactionID = row["Transaction ID"],
                        ItemName = row["Item Title"],
                        ItemCode = row["Item ID"]
                    });
        }

        public static ParsedData ParseIpn(NameValueCollection values) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            return new ParsedData()
                   {
                       Time = ConvertPayPalDateTime(values["payment_date"]),
                       Name = values["first_name"] + " " + values["last_name"],
                       Status = values["payment_status"],
                       Currency = values["mc_currency"],
                       Gross = double.Parse(values["mc_gross"]),
                       Net = double.Parse(values["mc_gross"]) - double.Parse(values["mc_fee"]),
                       Email = values["payer_email"],
                       TransactionID = values["txn_id"],
                       ItemName = values["item_name"],
                       ItemCode = values["item_number"]
                   };
        }

        public static bool TryParseItemCode(string itemCode, out int? accountID, out int? jarID) {
            if (!string.IsNullOrEmpty(itemCode)) {
                var match = Regex.Match(itemCode, "ZK_ID_([0-9]*)_JAR_([0-9]*)");
                if (match.Success) {
                    int i;
                    if (int.TryParse(match.Groups[1].Value, out i) && i != 0) accountID = i;
                    else accountID = null;
                    if (int.TryParse(match.Groups[2].Value, out i)) jarID = i;
                    else jarID = null;
                    return true;
                }
            }
            accountID = null;
            jarID = null;
            return false;
        }


        /// <summary>
        /// Sends request back to paypal to verify its true 
        /// </summary>
        /// <returns></returns>
        public static bool VerifyRequest(byte[] data) {
            var req = (HttpWebRequest)WebRequest.Create("https://www.paypal.com/cgi-bin/webscr");

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

        /// <summary>
        /// Records the payment. <paramref name="granted"/> is whether PayPal confirmed the
        /// notification: when it did not, the row is still written - an unverifiable payment is
        /// worth having in the table where a human can look at it - but none of the things a
        /// forged notification would be sent to obtain happen. HasKudos stays as it was, no
        /// redeem code is mailed out, and nothing is announced in the lobby.
        /// </summary>
        Contribution AddPayPalContribution(ParsedData parsed, bool granted = true) {
            try {
                if ((parsed.Status != "Completed" && parsed.Status != "Cleared") || parsed.Gross <= 0) return null; // not a contribution!

                double netEur;
                double grossEur;
                if (parsed.Currency == "EUR") {
                    netEur = parsed.Net;
                    grossEur = parsed.Gross;
                }
                else {
                    netEur = ConvertToEuros(parsed.Currency, parsed.Net);
                    grossEur = ConvertToEuros(parsed.Currency, parsed.Gross);
                }

                int? accountID, jarID;
                TryParseItemCode(parsed.ItemCode, out accountID, out jarID);
                
                Contribution contrib;
                using (var db = new ZkDataContext()) {
                    Account acc = null;
                    ContributionJar jar;
                    if (jarID == null) jar = db.ContributionJars.FirstOrDefault(x => x.IsDefault);
                    else {
                        jar = db.ContributionJars.FirstOrDefault(x => x.ContributionJarID == jarID);
                        if (jar == null) {
                            Trace.TraceError("jarID was invalid? {0}", jarID);
                            jar = db.ContributionJars.FirstOrDefault(x => x.IsDefault);
                        }
                    }

                    if (accountID != null) acc = db.Accounts.Find(accountID.Value);

                    if (!string.IsNullOrEmpty(parsed.TransactionID) && db.Contributions.Any(x => x.PayPalTransactionID == parsed.TransactionID)) return null; // contribution already exists
                    var isSpring = !parsed.ItemCode.StartsWith("ZK") || jar.IsDefault;


                    contrib = new Contribution()
                              {
                                  AccountByAccountID = acc,
                                  Name = parsed.Name,
                                  Euros = grossEur,
                                  KudosValue = (int)Math.Round(grossEur*GlobalConst.EurosToKudos),
                                  OriginalAmount = parsed.Gross,
                                  OriginalCurrency = parsed.Currency,
                                  PayPalTransactionID = parsed.TransactionID,
                                  ItemCode = parsed.ItemCode,
                                  Time = parsed.Time,
                                  EurosNet = netEur,
                                  ItemName = parsed.ItemName,
                                  Email = parsed.Email,
                                  RedeemCode = Guid.NewGuid().ToString(),
                                  IsSpringContribution = isSpring,
                                  ContributionJar = jar
                              };
                    db.Contributions.Add(contrib);

                    db.SaveChanges();

                    if (!granted) return contrib;

                    if (acc != null) acc.HasKudos = true;
                    db.SaveChanges();


                    // technically not needed to sent when account is known, but perhaps its nice to get a confirmation like that

                    SendEmail(contrib);

                    NewContribution(contrib);
                }

                return contrib;
            } catch (Exception ex) {
                Trace.TraceError("Error processing payment: {0}", ex);
                Error(ex.ToString());
                return null;
            }
        }

        public static void SendEmail(Contribution contrib) {
            var smtp = new SmtpClient("localhost");

            var subject = string.Format("Thank you for donating to {0}, redeem your Kudos now! :-)", contrib.IsSpringContribution  ? "Spring/Zero-K" : "Zero-K");

            var body =
                string.Format(
                    "Hi {0}, \nThank you for donating to {1}\nYou can now redeem Kudos - special reward for Zero-K by clicking here: {2} \n (Please be patient Kudos features for the game will be added in the short future)\n\nWe wish you lots of fun playing the game and we are looking forward to meet you in game!\nThe Zero-K team",
                    contrib.Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(),
                    contrib.IsSpringContribution ? "the Spring project and Zero-K" : "Zero-K and Spring project",
                    GetCodeLink(contrib.RedeemCode));

            smtp.Send(new MailMessage(GlobalConst.TeamEmail, contrib.Email, subject, body));
        }

        static string GetCodeLink(string code) {
            return string.Format(GlobalConst.BaseSiteUrl + "/Contributions/Redeem/{0}", code);
        }

        class ConvertResponse
        {
            public string from { get; set; }
            public double rate { get; set; }
            public string to { get; set; }
            public double v { get; set; }
        }

        public class ParsedData
        {
            public string Currency { get; set; }
            public string Email { get; set; }
            public double Gross { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string Name { get; set; }
            public double Net { get; set; }
            public string Status { get; set; }
            public DateTime Time { get; set; }
            public string TransactionID { get; set; }
        }
    }
}
