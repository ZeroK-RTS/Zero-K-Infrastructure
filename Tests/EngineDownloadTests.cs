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
        [TestMethod]
        public void GetDevelopList() {
            var list = EngineDownload.GetEngineList();
            Assert.IsTrue(list.Count > 1);
        }
    }
}
