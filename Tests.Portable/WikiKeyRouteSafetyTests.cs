using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared;

namespace Tests.Portable
{
    /// <summary>
    /// Why a wiki key can be put in a URL path at all.
    ///
    /// `Wiki/{node}` is the one route the site generates with a free-text segment - the segment is
    /// a ForumThread's WikiKey - and `Web.config` currently turns off the platform's path
    /// character filtering with `requestPathInvalidCharacters=""`. Whether that can go back to its
    /// default depends on whether any legitimate name contains a character the default rejects.
    ///
    /// For wiki keys the answer is no, and this is the reason: both places that set a WikiKey now
    /// run Account.IsValidLobbyName, whose charset is letters, digits, underscore and brackets.
    /// None of ASP.NET's default invalid path characters is in it, and neither is the slash that
    /// would split the segment.
    ///
    /// This test is the link between those two facts. If the charset is ever widened, the wiki
    /// route stops being safe to serve under the default setting, and that is worth failing over
    /// rather than discovering from a 400.
    /// </summary>
    [TestClass]
    public class WikiKeyRouteSafetyTests
    {
        /// <summary>ASP.NET's requestPathInvalidCharacters default.</summary>
        private const string DefaultInvalidPathCharacters = "<>*%&:\\?";

        [TestMethod]
        public void No_character_the_platform_rejects_in_a_path_is_a_valid_name_character()
        {
            foreach (var character in DefaultInvalidPathCharacters)
                Assert.IsFalse(Utils.ValidLobbyNameCharacter(character),
                    "'" + character + "' is in requestPathInvalidCharacters' default, so a name "
                    + "containing it could not be served as a /Wiki/{node} segment");
        }

        [TestMethod]
        public void Nor_is_a_slash_or_anything_else_that_would_leave_the_segment()
        {
            foreach (var character in "/#. ")
                Assert.IsFalse(Utils.ValidLobbyNameCharacter(character),
                    "'" + character + "' would end or escape the route segment");
        }

        [TestMethod]
        public void What_a_wiki_key_may_contain_is_still_what_it_was()
        {
            // Stated as an example as well as a property, so widening the charset shows up here as
            // a change to read rather than only as a failure above.
            foreach (var character in "abcxyzABCXYZ0189_[]")
                Assert.IsTrue(Utils.ValidLobbyNameCharacter(character),
                    "'" + character + "' has always been allowed in a name");
        }
    }
}
