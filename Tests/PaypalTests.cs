using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NightWatch;

namespace Tests
{
    [TestFixture]
    public class PaypalTests
    {
        // rate exchange http://rate-exchange.appspot.com/currency?from=USD&to=EUR&q=1


        [Test]
        public void CheckEmails() {
            var pp = new PayPalChecker();
            pp.CheckEmails();
        }

        [Test]
        public void CheckConversion() {
            Console.WriteLine("100 CZK in EUR = " + PayPalChecker.ConvertToEuros("CZK", 100));
        }


        [Test]
        public void CheckParsing() {
            var ret = PayPalChecker.ParseEmail(Resource.mail1);
            Assert.AreNotEqual(null, ret.ConfirmationNumber);
            Assert.AreNotEqual(null, ret.Contributor);
            Assert.AreNotEqual(null, ret.Currency);
            Assert.AreNotEqual(null, ret.Reference);
            Console.WriteLine("Payment by {0} of {1} {2}, reference {3}, confirmation: {4}, message: {5}",ret.Contributor, ret.Amount, ret.Currency, ret.Reference, ret.ConfirmationNumber, ret.Message);


            
            ret = PayPalChecker.ParseEmail(Resource.mail2);
            Assert.AreNotEqual(null, ret.ConfirmationNumber);
            Assert.AreNotEqual(null, ret.Contributor);
            Assert.AreNotEqual(null, ret.Currency);
            Assert.AreNotEqual(null, ret.Reference);
            Console.WriteLine("Payment by {0} of {1} {2}, reference {3}, confirmation: {4}, message: {5}", ret.Contributor, ret.Amount, ret.Currency, ret.Reference, ret.ConfirmationNumber, ret.Message);

            ret = PayPalChecker.ParseEmail(Resource.mail3);
            Assert.AreNotEqual(null, ret.ConfirmationNumber);
            Assert.AreNotEqual(null, ret.Contributor);
            Assert.AreNotEqual(null, ret.Currency);
            Assert.AreNotEqual(null, ret.Reference);
            Console.WriteLine("Payment by {0} of {1} {2}, reference {3}, confirmation: {4}, message: {5}", ret.Contributor, ret.Amount, ret.Currency, ret.Reference, ret.ConfirmationNumber, ret.Message);

            ret = PayPalChecker.ParseEmail(Resource.mail4);
            Assert.AreNotEqual(null, ret.ConfirmationNumber);
            Assert.AreNotEqual(null, ret.Contributor);
            Assert.AreNotEqual(null, ret.Currency);
            Assert.AreNotEqual(null, ret.Reference);
            Console.WriteLine("Payment by {0} of {1} {2}, reference {3}, confirmation: {4}, message: {5}", ret.Contributor, ret.Amount, ret.Currency, ret.Reference, ret.ConfirmationNumber, ret.Message);

            ret = PayPalChecker.ParseEmail(Resource.mail5);
            Assert.AreNotEqual(null, ret.ConfirmationNumber);
            Assert.AreNotEqual(null, ret.Contributor);
            Assert.AreNotEqual(null, ret.Currency);
            Assert.AreNotEqual(null, ret.Reference);
            Console.WriteLine("Payment by {0} of {1} {2}, reference {3}, confirmation: {4}, message: {5}", ret.Contributor, ret.Amount, ret.Currency, ret.Reference, ret.ConfirmationNumber, ret.Message);


        }
    }
}
