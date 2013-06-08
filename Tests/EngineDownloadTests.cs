using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PlasmaDownloader;

namespace Tests
{
    [TestFixture]
    public class EngineDownloadTests
    {
        public void GetDevelopList() {
            var list = EngineDownload.GetEngineList();
            Assert.IsTrue(list.Count > 1);
        }
    }
}
