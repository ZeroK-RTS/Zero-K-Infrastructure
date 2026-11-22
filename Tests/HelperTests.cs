using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared;

namespace Tests
{
    [TestClass]
    public class HelperTests
    {
        // [TestMethod("Basic")] until 2026-10-01, which looks like a category and is not one -
        // that argument is the test's DISPLAY NAME. The Windows job filters on
        // TestCaseFilter:TestCategory=Basic, so this never ran there, and the annotation said it
        // did. It is right that it does not run: GetMyIpAddress asks an external service what this
        // machine's address is. Adding the category would put that call in CI.
        [TestMethod]
        public void TestIpHelpers()
        {
            var ip = IpHelpers.GetMyIpAddress();
            Assert.IsTrue(!string.IsNullOrEmpty(ip));


            var parsed = IPAddress.Parse(ip);
            Assert.IsTrue(parsed.MapToIPv6().ToString() != ip);
        }
        
    }
}