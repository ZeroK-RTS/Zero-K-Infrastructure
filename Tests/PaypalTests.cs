using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LobbyClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZkData;

namespace Tests
{
    [TestClass]
    public class PaypalTests
    {
        [TestMethod]
        public void CheckConversion() {
            //var teststr=  JsonSerializer.SerializeToString(new ProtocolExtension.JugglerConfig() { Active = true });
            //var acc = Account.AccountByName(new ZkDataContext(), "Clogger");
            //Console.WriteLine(AuthTools.GetSiteAuthToken(acc.Name, acc.Password, DateTime.Now.AddDays(-1)));

            var czk100InEur = PayPalInterface.ConvertToEuros("CZK", 100);
            Assert.IsTrue(czk100InEur > 1 && czk100InEur < 10); 
        }

    }
}
