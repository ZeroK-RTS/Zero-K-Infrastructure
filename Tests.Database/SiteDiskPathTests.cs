using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared;
using ZkData;

namespace Tests.Database
{
    /// <summary>
    /// GlobalConst.SiteDiskPath, and what happens when nothing has set it.
    ///
    /// The website sets it in Application_Start - GlobalConst.SiteDiskPath = MapPath("~") - so
    /// inside the web application the default never applied. Every other process gets the
    /// default, and that set grew when the lobby server moved out of the website's IIS worker:
    /// ServerBattle asks MetaDataCache.ServerGetMod and ServerGetMap for the hosted game and map
    /// whenever a battle opens, and a standalone server inherits no assignment at all.
    ///
    /// Both lookups returned null for a file they could not find and said nothing about it, which
    /// surfaces to a player as "this game has no options" from !listoptions - a believable answer
    /// to a misconfigured path. These tests pin the two things that must hold for the quiet path
    /// to stay quiet only when it should: no exception, and null rather than a guess.
    /// </summary>
    [TestClass]
    public class SiteDiskPathTests
    {
        private static T WithSiteDiskPath<T>(string path, Func<T> body)
        {
            var original = GlobalConst.SiteDiskPath;
            GlobalConst.SiteDiskPath = path;
            try { return body(); }
            finally { GlobalConst.SiteDiskPath = original; }
        }

        /// <summary>
        /// The default off Windows is empty, deliberately - the site's directory is wherever it
        /// was deployed and there is nothing to guess. Empty has to be survivable rather than an
        /// ArgumentNullException out of Path.Combine on the first battle a server opens.
        /// </summary>
        [TestMethod]
        public void An_unset_site_path_is_answered_with_null_rather_than_thrown_on()
        {
            Assert.IsNull(WithSiteDiskPath("", () => MetaDataCache.ServerGetMod("zk:stable")));
            Assert.IsNull(WithSiteDiskPath("", () => MetaDataCache.ServerGetMap("Small_Divide")));
        }

        [TestMethod]
        public void A_site_path_with_no_metadata_in_it_is_answered_with_null()
        {
            var empty = Path.Combine(Path.GetTempPath(), "zk-site-disk-path-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(empty);
            try
            {
                Assert.IsNull(WithSiteDiskPath(empty, () => MetaDataCache.ServerGetMod("zk:stable")));
                Assert.IsNull(WithSiteDiskPath(empty, () => MetaDataCache.ServerGetMap("Small_Divide")));
            }
            finally
            {
                Directory.Delete(empty, true);
            }
        }

        /// <summary>
        /// The Windows default is not a placeholder: it is where a developer's checkout puts the
        /// site, and AutoRegistrator and Fixer are run by hand against it. Off Windows the same
        /// string is not a path - there is no c: drive and a backslash is an ordinary filename
        /// character - so it would have been created as one DIRECTORY in the working directory.
        /// </summary>
        [TestMethod]
        public void The_default_is_a_windows_path_only_where_that_means_something()
        {
            var isUnix = Environment.OSVersion.Platform == PlatformID.Unix;
            var configured = Environment.GetEnvironmentVariable("ZK_SITE_DISK_PATH");
            if (!string.IsNullOrEmpty(configured)) Assert.Inconclusive("ZK_SITE_DISK_PATH is set");

            if (isUnix) Assert.IsFalse(GlobalConst.SiteDiskPath.Contains("\\"),
                "the default still carries a Windows path off Windows: " + GlobalConst.SiteDiskPath);
            else Assert.IsTrue(GlobalConst.SiteDiskPath.Contains("\\"),
                "the Windows default moved, which relocates where a deployed site is read from");
        }
    }
}
