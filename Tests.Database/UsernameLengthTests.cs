using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared;
using ZkData;

namespace Tests.Database
{
    /// <summary>
    /// Three numbers describe how long an account name may be, and only the smallest is enforced
    /// where names come in:
    ///
    ///   GlobalConst.MaxUsernameLength   25     Account.IsValidLobbyName, checked server-side at
    ///                                          registration (LoginChecker.DoRegister) and at
    ///                                          rename (UsersController)
    ///   [StringLength] on Account.Name  200    EF6 validation at SaveChanges, reproduced for the
    ///                                          port in ZkData.Core/Ef6Compat/EntityValidation.cs
    ///   the column                      2000   what the EF6 migrations actually built, and what
    ///                                          the port models explicitly in ColumnFacets
    ///
    /// **That spread is fine, and was recorded as "schema drift worth acting on" when only the
    /// last two were known.** Ordered smallest-first it is defence in depth: nothing can reach the
    /// database that the entry points would refuse.
    ///
    /// What is not fine is the order changing. Raise MaxUsernameLength past 200 and registration
    /// starts accepting names that SaveChanges then rejects - a failure at the wrong layer, with a
    /// validation message about a column rather than about the name somebody typed. Nothing
    /// guarded that until this test.
    /// </summary>
    [TestClass]
    public class UsernameLengthTests
    {
        private static int AttributeLimit()
        {
            var property = typeof(Account).GetProperty("Name");
            var attribute = property.GetCustomAttributes(typeof(StringLengthAttribute), false)
                .Cast<StringLengthAttribute>()
                .FirstOrDefault();

            Assert.IsNotNull(attribute, "Account.Name has no [StringLength] - the middle of the chain is gone");
            return attribute.MaximumLength;
        }

        [TestMethod]
        public void A_name_the_entry_points_accept_can_always_be_saved()
        {
            Assert.IsTrue(GlobalConst.MaxUsernameLength <= AttributeLimit(),
                string.Format(
                    "MaxUsernameLength is {0} but Account.Name is [StringLength({1})]: registration "
                    + "would accept a name that SaveChanges then refuses, and the error would talk "
                    + "about a column rather than about the name",
                    GlobalConst.MaxUsernameLength, AttributeLimit()));
        }

        [TestMethod]
        public void A_name_that_validation_accepts_fits_the_column()
        {
            using (var db = new ZkDataContext())
            {
                var width = db.Database.SqlQuery<int?>(
                    "SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS "
                    + "WHERE TABLE_NAME = 'Accounts' AND COLUMN_NAME = 'Name'").FirstOrDefault();

                Assert.IsNotNull(width, "Accounts.Name is not there, which is a bigger problem than this test");
                Assert.IsTrue(AttributeLimit() <= width.Value,
                    string.Format("[StringLength({0})] is wider than the column ({1}): validation would "
                                  + "pass and the insert would fail", AttributeLimit(), width.Value));
            }
        }

        [TestMethod]
        public void No_stored_name_is_longer_than_validation_allows()
        {
            // Against the fixture this is trivially true - the names are player01..player30. It is
            // here for the run that matters: pointed at a restored copy of the live database, it
            // answers the question the drift note could only speculate about, which is whether
            // anything already in the table predates the limits above.
            using (var db = new ZkDataContext())
            {
                var longest = db.Accounts.Select(x => x.Name.Length).OrderByDescending(x => x).FirstOrDefault();

                Assert.IsTrue(longest <= AttributeLimit(),
                    string.Format("a stored name is {0} characters, past the [StringLength({1})] that "
                                  + "EF6 validation enforces - so that row cannot be saved again "
                                  + "without being renamed", longest, AttributeLimit()));
                Console.WriteLine("        longest stored name: {0} characters (limit {1}, entry points {2})",
                    longest, AttributeLimit(), GlobalConst.MaxUsernameLength);
            }
        }
    }
}
