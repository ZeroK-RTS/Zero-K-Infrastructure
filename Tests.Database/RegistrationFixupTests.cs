using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZkData;

namespace Tests.Database
{
    /// <summary>
    /// Registration inserts an Account and two dependents in one SaveChanges, and it links them by
    /// copying a primary key that does not exist yet.
    ///
    /// LoginChecker.DoRegister builds `acc`, then calls LogIP and LogUserID, then Adds `acc`. Both
    /// helpers do `new AccountIP { AccountID = acc.AccountID, ... }` - and at that moment
    /// acc.AccountID is 0, because the database has not assigned one. The navigation property is
    /// never set. On paper nothing connects the dependent row to the account.
    ///
    /// **Under EF6 it works anyway, and this test is here to say why.** EF6 leaves a store-generated
    /// int key at its CLR default until save, so the Added Account's key IS 0 - and relationship
    /// fixup matches the dependent's foreign key 0 to it. The two rows are related by an accident of
    /// which sentinel value EF6 picked. EF Core picks negative temporary values instead, nothing
    /// matches 0, and SaveChanges throws "The value of 'AccountUserID.AccountID' is unknown".
    ///
    /// That is not hypothetical: it is what the .NET 9 lobby server did the first time a client
    /// tried to register through it. The fix sets the navigation property, which is how both stacks
    /// are meant to be told. This test guards the EF6 half of that - if setting the navigation ever
    /// broke registration on the stack still serving players, it fails here.
    /// </summary>
    [TestClass]
    public class RegistrationFixupTests
    {
        // One per test, because Accounts.Name is uniquely indexed (IX_Name) and this runner calls
        // [TestInitialize] once for the class rather than once per method.
        private const string FkOnlyName = "fixup_probe_fk_only";
        private const string NavigationName = "fixup_probe_navigation";

        // The probe's own marker values, which no fixture row uses.
        private static readonly long[] ProbeUserIDs = { 987654321L, 987654322L };
        private static readonly string[] ProbeIPs = { "203.0.113.7", "203.0.113.8" };

        /// <summary>
        ///     Deletes by the marker values rather than by account name: a run that failed
        ///     halfway can leave dependents whose account is already gone, and those are exactly
        ///     what makes the next run fail for the wrong reason.
        /// </summary>
        [TestCleanup]
        [TestInitialize]
        public void RemoveTheProbeRows()
        {
            using (var db = new ZkDataContext())
            {
                db.AccountUserIDs.RemoveRange(db.AccountUserIDs.Where(x => ProbeUserIDs.Contains(x.UserID)));
                db.AccountIPs.RemoveRange(db.AccountIPs.Where(x => ProbeIPs.Contains(x.IP)));
                db.SaveChanges();

                db.Accounts.RemoveRange(db.Accounts.Where(x => x.Name == FkOnlyName || x.Name == NavigationName));
                db.SaveChanges();
            }
        }

        /// <summary>
        ///     The shape production runs today: the foreign key is copied and the navigation is
        ///     never set. It passes under EF6, which is the whole puzzle - and the reason is the
        ///     sentinel value, not the code.
        /// </summary>
        [TestMethod]
        public void EF6_links_them_by_foreign_key_alone_because_its_unsaved_key_is_also_zero()
        {
            Assert.AreEqual(0, new Account().AccountID,
                "an unsaved Account's key is not 0, so the coincidence this test explains is gone "
                + "and LoginChecker's foreign-key-only linking never worked");

            var accountId = SaveRegistration(FkOnlyName, setNavigation: false, userId: 987654321, ip: "203.0.113.7");
            AssertLinked(accountId, 987654321, "203.0.113.7");
        }

        /// <summary>
        ///     The shape after the fix: the navigation property says what is related, which is what
        ///     both stacks actually read. EF6 must keep working, or the fix breaks the server that
        ///     is serving players today.
        /// </summary>
        [TestMethod]
        public void EF6_still_links_them_when_the_navigation_property_is_set_instead()
        {
            var accountId = SaveRegistration(NavigationName, setNavigation: true, userId: 987654322, ip: "203.0.113.8");
            AssertLinked(accountId, 987654322, "203.0.113.8");
        }

        private static int SaveRegistration(string name, bool setNavigation, long userId, string ip)
        {
            using (var db = new ZkDataContext())
            {
                var acc = new Account { Name = name };
                acc.SetName(name);
                acc.SetPasswordHashed("not-a-real-hash");
                acc.SetAvatar();

                // LoginChecker's order: the dependents are built and added BEFORE the account is,
                // carrying a key that is still 0.
                var userIdRow = new AccountUserID { AccountID = acc.AccountID, UserID = userId, InstallID = "", FirstLogin = DateTime.UtcNow, LastLogin = DateTime.UtcNow, LoginCount = 1 };
                var ipRow = new AccountIP { AccountID = acc.AccountID, IP = ip, FirstLogin = DateTime.UtcNow, LastLogin = DateTime.UtcNow, LoginCount = 1 };

                if (setNavigation)
                {
                    acc.AccountUserIDs.Add(userIdRow);
                    acc.AccountIPs.Add(ipRow);
                }

                db.AccountUserIDs.InsertOnSubmit(userIdRow);
                db.AccountIPs.InsertOnSubmit(ipRow);

                db.Accounts.Add(acc);
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    // The runner prints ex.Message, and EF6's outer message is always the same
                    // uninformative sentence. Say what actually went wrong.
                    var inner = ex;
                    var detail = "";
                    while (inner != null) { detail += "\n          " + inner.GetType().Name + ": " + inner.Message; inner = inner.InnerException; }
                    throw new Exception("SaveChanges failed:" + detail);
                }

                Assert.AreNotEqual(0, acc.AccountID, "the account saved without getting an identity, which makes the rest meaningless");
                return acc.AccountID;
            }
        }

        private static void AssertLinked(int accountId, long userId, string ip)
        {
            // A second context, because the first one's fixup could make a wrong row look right.
            using (var db = new ZkDataContext())
            {
                var savedUserId = db.AccountUserIDs.SingleOrDefault(x => x.UserID == userId);
                Assert.IsNotNull(savedUserId, "the AccountUserID row is not there at all");
                Assert.AreEqual(accountId, savedUserId.AccountID,
                    "the AccountUserID saved against the wrong account - the dependent was written "
                    + "before the key it points at existed, and nothing corrected it");

                var savedIp = db.AccountIPs.SingleOrDefault(x => x.IP == ip);
                Assert.IsNotNull(savedIp, "the AccountIP row is not there at all");
                Assert.AreEqual(accountId, savedIp.AccountID, "the AccountIP saved against the wrong account");
            }
        }

    }
}
