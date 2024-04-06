using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared;

namespace Tests
{
    [TestClass]
    public class HelperTests
    {
        [TestMethod("Basic")]
        public void TestIpHelpers()
        {
            var ip = IpHelpers.GetMyIpAddress();
            Assert.IsTrue(!string.IsNullOrEmpty(ip));


            var parsed = IPAddress.Parse(ip);
            Assert.IsTrue(parsed.MapToIPv6().ToString() != ip);
        }
        
    }
}