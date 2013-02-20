using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Timers;
using Newtonsoft.Json;
using OpenPop.Mime;
using OpenPop.Pop3;
using ZkData;

namespace NightWatch
{
    public class PayPalChecker
    {
        const int EmailCheckSeconds = 60;
        public const double EurosToKudos = 10.0;
        public const string PayPalReferencePrefix = "ZK_ID_";
        const double conversionMultiplier = 0.98; // extra cost of conversion from foreign currency
        const string login = "paypal";
        const string password = "supersecret1";
        readonly Timer timer;

        public event Action<Exception> Error = (e) => { };
        public event Action<Contribution> NewContribution = (c) => { };

        public PayPalChecker() {
            timer = new Timer(EmailCheckSeconds*1000) { AutoReset = true };
            timer.Elapsed += (sender, args) =>
                {
                    try {
                        timer.Stop();
                        CheckEmails();
                    } catch (Exception ex) {
                        Trace.TraceError("Error processing emails: {0}", ex);
                        Error(ex);
                    } finally {
                        timer.Start();
                    }
                };
        }


        public void CheckEmails() {
            using (var popClient = new Pop3Client()) {
                popClient.Connect("mail.licho.eu", 995, true, 10000, 10000, (sender, certificate, chain, errors) => { return true; });
                popClient.Authenticate(login, password);

                var count = popClient.GetMessageCount();
                for (var i = 1; i <= count; i++) {
                    var message = popClient.GetMessage(i);
                    if (ProcessPayPalEmail(message)) popClient.DeleteMessage(i);
                }
                popClient.Disconnect();
            }
        }


        public static double ConvertToEuros(string fromCurrency, double fromAmount) {
            using (var wc = new WebClient()) {
                var response =
                    wc.DownloadString(string.Format("http://rate-exchange.appspot.com/currency?from={0}&to=EUR&q={1}", fromCurrency, fromAmount));
                var ret = JsonConvert.DeserializeObject<ConvertResponse>(response);
                return ret.v*conversionMultiplier;
            }
        }

        public static ParsedEmail ParseEmail(string text) {
            var ret = new ParsedEmail();
            var match = Regex.Match(text, "Total amount:[^0-9]*([0-9\\.]+) ([A-Z]+)");
            ret.Amount = Double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            ret.Currency = match.Groups[2].Value;

            match = Regex.Match(text, "Confirmation number:[^\\w]*([\\w]+)");
            ret.ConfirmationNumber = match.Groups[1].Value;

            match = Regex.Match(text, "Reference:[^\\w]*([^\\r\\n]+)");
            ret.Reference = match.Groups[1].Value;

            match = Regex.Match(text, "Contributor:[^\\w]*([^\\r\\n]+)");
            ret.Contributor = match.Groups[1].Value;

            match = Regex.Match(text, "Message:[^\\w]*([^\\r\\n]+)");
            if (match.Success) ret.Message = match.Groups[1].Value;

            return ret;
        }

        public void StartChecking() {
            timer.Start();
        }

        public void StopChecking() {
            timer.Stop();
        }

        bool ProcessPayPalEmail(Message message) {
            try {
                var text = message.FindFirstPlainTextVersion().GetBodyAsText();

                var parsed = ParseEmail(text);
                double euros;
                if (parsed.Currency == "EUR") euros = parsed.Amount;
                else euros = ConvertToEuros(parsed.Currency, parsed.Amount);

                int? accountID = null;
                if (parsed.Reference != null) {
                    var match = Regex.Match(parsed.Reference, string.Format("{0}([0-9]+)", PayPalReferencePrefix));
                    if (match.Success) accountID = int.Parse(match.Groups[1].Value);
                }

                using (var db = new ZkDataContext()) {
                    Account acc = null;
                    if (accountID != null) acc = Account.AccountByAccountID(db, accountID.Value);

                    if (!string.IsNullOrEmpty(parsed.ConfirmationNumber) &&
                        db.Contributions.Any(x => x.PayPalTransactionID == parsed.ConfirmationNumber)) throw new ApplicationException(string.Format("Contribution {0} already exists", parsed.ConfirmationNumber));

                    var contrib = new Contribution()
                                  {
                                      Account = acc,
                                      Name = parsed.Contributor,
                                      Euros = euros,
                                      KudosValue = (int)Math.Round(euros*EurosToKudos),
                                      OriginalAmount = parsed.Amount,
                                      OriginalCurrency = parsed.Currency,
                                      PayPalTransactionID = parsed.ConfirmationNumber,
                                      ItemCode = parsed.Reference,
                                      Time = message.Headers.DateSent,
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

        class ConvertResponse
        {
            public string from;
            public double rate;
            public string to;
            public double v;
        }

        public class ParsedEmail
        {
            public double Amount;
            public string ConfirmationNumber;
            public string Contributor;
            public string Currency;
            public string Message;
            public string Reference;
        }
    }
}