using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LobbyClient;
using NUnit.Framework;
using NightWatch;
using PlasmaShared;
using ServiceStack.Text;
using ZkData;

namespace Tests
{
    [TestFixture]
    public class PaypalTests
    {
        
        
        [Test]
        public void CheckConversion() {
            //var teststr=  JsonSerializer.SerializeToString(new ProtocolExtension.JugglerConfig() { Active = true });
            //var acc = Account.AccountByName(new ZkDataContext(), "Clogger");
            //Console.WriteLine(AuthTools.GetSiteAuthToken(acc.Name, acc.Password, DateTime.Now.AddDays(-1)));

            Console.WriteLine("100 CZK in EUR = " + PayPalInterface.ConvertToEuros("CZK", 100));
        }

    }
}
