using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared;
using ZkData;

namespace Tests.Database
{
    /// <summary>
    /// A mission's image is written by MissionUpdater and served back by URL, and the two have to
    /// name the same place:
    ///
    ///   MissionUpdater.GetMissionImagePath   SiteDiskPath/img/missions/{id}.png
    ///   ContentServiceImplementation         {BaseSiteUrl}/img/missions/{id}.png
    ///   Missions/Detail.cshtml, TileList     ~/img/missions/{id}.png
    ///
    /// The write used to be `imgPath + "{0}.png"` over a variable ending in a separator. When that
    /// variable became a Path.Combine it stopped ending in one, and the image landed at
    /// img/missions123.png - beside the directory, not in it. The directory was still created and
    /// nothing threw, so the only symptom was a broken image on a page nobody checks in CI.
    ///
    /// These do not restate the helper. The first says the file lands INSIDE the directory rather
    /// than beside it, which is the defect; the second says the last three segments are what every
    /// reader asks for.
    /// </summary>
    [TestClass]
    public class MissionImagePathTests
    {
        private static T WithSiteDiskPath<T>(string path, Func<T> body)
        {
            var original = GlobalConst.SiteDiskPath;
            GlobalConst.SiteDiskPath = path;
            try { return body(); }
            finally { GlobalConst.SiteDiskPath = original; }
        }

        [TestMethod]
        public void The_image_lands_inside_the_missions_directory_not_beside_it()
        {
            var root = Path.Combine(Path.GetTempPath(), "zk-site");
            var file = WithSiteDiskPath(root, () => MissionUpdater.GetMissionImagePath(123));

            var directory = Path.GetDirectoryName(file);
            Assert.AreEqual(Path.Combine(root, "img", "missions"), directory,
                "the image is written beside the missions directory rather than into it");
            Assert.AreEqual("123.png", Path.GetFileName(file));
        }

        /// <summary>
        /// The URL every reader uses, spelled out rather than referenced: ContentServiceImplementation
        /// and the two mission views live in the web project, which cannot be referenced from here.
        /// </summary>
        [TestMethod]
        public void The_path_ends_the_way_the_url_that_serves_it_does()
        {
            var file = WithSiteDiskPath("/site", () => MissionUpdater.GetMissionImagePath(7));
            var expected = Path.Combine("/site", "img", "missions", "7.png");

            Assert.AreEqual(expected, file,
                "img/missions/{id}.png is what ContentServiceImplementation hands the game client "
                + "and what Missions/Detail.cshtml renders; the file has to be there");
        }
    }
}
