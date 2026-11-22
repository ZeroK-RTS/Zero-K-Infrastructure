using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaDownloader;

namespace Tests
{
    [TestClass]
    public class EngineDownloadTests
    {
        // No TestCategory("Basic"), on purpose: the Windows CI job runs
        // /TestCaseFilter:TestCategory=Basic, and this test calls out to the network.
        [TestMethod]
        public void GetDevelopList() {
            var list = EngineDownload.GetEngineList();
            Assert.IsTrue(list.Count > 1);
        }
    }
}
