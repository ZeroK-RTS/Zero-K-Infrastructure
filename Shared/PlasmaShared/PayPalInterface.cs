using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using ZkData;

namespace PlasmaShared
{
    public class PayPalInterface
    {
        public const string PayPalReferencePrefix = "ZK_ID_";
        const double conversionMultiplier = 0.98; // extra cost of conversion from foreign currency

        public event Action<Exception> Error = (e) => { };
        public event Action<Contribution> NewContribution = (c) => { };


        public static double ConvertToEuros(string fromCurrency, double fromAmount) {
            using (var wc = new WebClient()) {
                var response =
                    wc.DownloadString(string.Format("http://rate-exchange.appspot.com/currency?from={0}&to=EUR&q={1}", fromCurrency, fromAmount));
                var ret = JsonConvert.DeserializeObject<ConvertResponse>(response);
                return ret.v*conversionMultiplier;
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


        bool AddPayPalContribution(ParsedData parsed) {
            try {
                if (parsed.Status != "Completed" || parsed.Gross <= 0) return false; // not a contribution!

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

                int? accountID = null;
                if (parsed.ItemCode != null) {
                    var match = Regex.Match(parsed.ItemCode, string.Format("{0}([0-9]+)", PayPalReferencePrefix));
                    if (match.Success) accountID = int.Parse(match.Groups[1].Value);
                }

                using (var db = new ZkDataContext()) {
                    Account acc = null;
                    if (accountID != null) acc = Account.AccountByAccountID(db, accountID.Value);

                    if (!string.IsNullOrEmpty(parsed.TransactionID) && db.Contributions.Any(x => x.PayPalTransactionID == parsed.TransactionID)) return false; // contribution already exists

                    var contrib = new Contribution()
                                  {
                                      Account = acc,
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
                                      Email = parsed.Email
                                  };
                    db.Contributions.InsertOnSubmit(contrib);
                    if (acc != null) acc.Kudos += contrib.KudosValue;
                    db.SubmitChanges();

                    NewContribution(contrib);
                }

                return true;
            } catch (Exception ex) {
                Trace.TraceError("Error processing payment: {0}", ex);
                Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Sends request back to paypal to verify its true 
        /// </summary>
        /// <returns></returns>
        public static bool VerifyRequest(byte[] data) {
            var req = (HttpWebRequest)WebRequest.Create("https://www.sandbox.paypal.com/cgi-bin/webscr");

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

        class ConvertResponse
        {
            public string from;
            public double rate;
            public string to;
            public double v;
        }

        public class ParsedData
        {
            public string Currency;
            public string Email;
            public double Gross;
            public string ItemCode;
            public string ItemName;
            public string Name;
            public double Net;
            public string Status;
            public DateTime Time;
            public string TransactionID;
        }

        public static ParsedData ParseIpn(NameValueCollection values)
        {
            return new ParsedData()
            {
                Time = DateTime.Parse(values["payment_date"]),
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
    }
}