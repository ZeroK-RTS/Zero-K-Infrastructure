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
        // No TestCategory("Basic"), on purpose: the Windows CI job runs
        // /TestCaseFilter:TestCategory=Basic, and this one both queries whois over the network and
        // asserts on registry data for two real IP addresses, which other people can change.
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
