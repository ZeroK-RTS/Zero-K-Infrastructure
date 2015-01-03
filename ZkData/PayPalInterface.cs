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
using ServiceStack.Text;
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
                var ret = JsonSerializer.DeserializeFromString<ConvertResponse>(response);
                return ret.v*conversionMultiplier;
            }
        }

        public static string GetItemCode(int? accountID, int? jarID) {
            return string.Format("ZK_ID_{0}_JAR_{1}", accountID, jarID);
        }

        public void ImportIpnPayment(NameValueCollection values, byte[] rawRequest) {
            try {
                var parsed = ParseIpn(values);
                var contribution = AddPayPalContribution(parsed);
                var verified = VerifyRequest(rawRequest);
                if (contribution != null && !verified) {
                    Error(
                        string.Format(
                            "Warning, transaction {0} by {1} VERIFICATION FAILED, check that it is not a fake! http://zero-k.info/Contributions ",
                            parsed.TransactionID,
                            parsed.Name));
                    using (var db = new ZkDataContext()) {
                        db.Contributions.First(x => x.ContributionID == contribution.ContributionID).Comment = "VERIFICATION FAILED";
                        db.SubmitAndMergeChanges();
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
                    if (int.TryParse(match.Groups[1].Value, out i)) accountID = i;
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

        Contribution AddPayPalContribution(ParsedData parsed) {
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
                    else jar = db.ContributionJars.FirstOrDefault(x => x.ContributionJarID == jarID);

                    if (accountID != null) acc = Account.AccountByAccountID(db, accountID.Value);

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
                    
                    db.SubmitAndMergeChanges();

                    if (acc != null) acc.Kudos = acc.KudosGained - acc.KudosSpent;
                    db.SubmitAndMergeChanges();


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
            return string.Format("http://zero-k.info/Contributions/Redeem/{0}", code);
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