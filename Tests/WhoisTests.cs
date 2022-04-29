using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZkData;

namespace Tests
{
    [TestClass]
    public class WhoisTests
    {
        [TestMethod]
        public async Task RunQuery() {
            var whois = new Whois();
            var data = whois.QueryByIp("31.7.187.232");
            Assert.AreEqual("OXYGEM", data["netname"]);

            data = whois.QueryByIp("62.233.34.238");
            Assert.AreEqual("info@cloudnovi.com", data["abuse-mailbox"]);
        }

    }
}
