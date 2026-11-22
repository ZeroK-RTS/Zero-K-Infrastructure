using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared;
using ZkData;

namespace Tests.Database
{
    /// <summary>
    /// The resources directory is written by one process and read by three others, and until now
    /// they did not all spell it the same way:
    ///
    ///   PlasmaServer.StoreMetadata   MapPath("~/Resources")            the writer
    ///   MetaDataCache                Path.Combine(.., "resources")     the lobby server's disk read
    ///   MissionUpdater               SiteDiskPath + @"\resources\"     and it CREATES the directory
    ///   Fixer                        SiteDiskPath + @"\Resources"
    ///
    /// On NTFS that is one directory and the spread cost nothing. Off Windows it is three, and
    /// nothing would have said so: the lobby server's disk lookup would have missed every time
    /// and fallen through to HTTP, which works, and a mission upload would have created a second
    /// directory that the site does not serve.
    ///
    /// These pin the two halves that have to agree. Neither is a restatement of the constant: the
    /// first says the URL the game client downloads from names the same directory the site writes
    /// to, and the second says a path built for the lobby server lands in it.
    /// </summary>
    [TestClass]
    public class ResourceFolderTests
    {
        [TestMethod]
        public void The_url_resources_are_served_from_names_the_directory_they_are_written_to()
        {
            var lastSegment = GlobalConst.ResourceBaseUrl.Split('/').Last();
            Assert.AreEqual(GlobalConst.ResourceFolder, lastSegment,
                "ResourceBaseUrl and the directory on disk have drifted apart: the client would "
                + "download from a URL that does not name the folder the site writes to");
        }

        /// <summary>
        /// Case matters here and only here. The site is deployed on Windows today and the port
        /// runs on Linux, so this is the assertion that would have caught the original spread -
        /// and it has to compare ORDINALLY, because a case-insensitive compare is exactly the
        /// assumption that made the bug invisible.
        /// </summary>
        [TestMethod]
        public void The_folder_is_spelled_the_way_the_writer_spells_it()
        {
            // "Resources" is not a preference, it is what PlasmaServer.StoreMetadata already put
            // on every deployed site: Global.MapPath("~/Resources"). That project cannot be
            // referenced from here, so the string is repeated once, under this comment, rather
            // than asserted against the writer itself.
            Assert.IsTrue(StringComparer.Ordinal.Equals("Resources", GlobalConst.ResourceFolder),
                "the resource folder is spelled '" + GlobalConst.ResourceFolder + "', and "
                + "PlasmaServer.StoreMetadata writes 'Resources'. On Linux those are two "
                + "directories: the site would serve one and read the other.");
        }
    }
}
