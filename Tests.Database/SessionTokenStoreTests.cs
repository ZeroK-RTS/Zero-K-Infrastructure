using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZkLobbyServer;

namespace Tests.Database
{
    /// <summary>
    /// Single sign-on tokens and their lifetime.
    ///
    /// The store takes its clock, so expiry is tested by moving time rather than by waiting -
    /// a test that sleeps for a real lifetime either takes hours or tests a lifetime nobody uses.
    /// </summary>
    [TestClass]
    public class SessionTokenStoreTests
    {
        private DateTime now = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

        private SessionTokenStore Store(TimeSpan? lifetime = null) =>
            new SessionTokenStore(lifetime ?? TimeSpan.FromHours(24), () => now);

        [TestMethod]
        public void A_fresh_token_redeems_to_its_account()
        {
            var store = Store();
            store.Add("tok", 42);

            Assert.AreEqual(42, store.Redeem("tok"));
        }

        [TestMethod]
        public void A_token_is_single_use()
        {
            var store = Store();
            store.Add("tok", 42);

            Assert.AreEqual(42, store.Redeem("tok"));
            Assert.IsNull(store.Redeem("tok"), "a second redemption must not work");
        }

        [TestMethod]
        public void An_expired_token_does_not_redeem()
        {
            var store = Store(TimeSpan.FromHours(24));
            store.Add("tok", 42);

            now = now.AddHours(24).AddSeconds(1);

            Assert.IsNull(store.Redeem("tok"));
        }

        [TestMethod]
        public void A_token_is_good_right_up_to_the_lifetime()
        {
            var store = Store(TimeSpan.FromHours(24));
            store.Add("tok", 42);

            now = now.AddHours(24);

            Assert.AreEqual(42, store.Redeem("tok"), "the boundary itself is still valid");
        }

        [TestMethod]
        public void An_expired_token_is_consumed_too()
        {
            // Leaving it would make redemption a way to ask whether a token exists, which is a
            // question an attacker with a stolen log would like answered.
            var store = Store(TimeSpan.FromHours(1));
            store.Add("tok", 42);
            now = now.AddHours(2);

            Assert.IsNull(store.Redeem("tok"));
            now = now.AddHours(-2);
            Assert.IsNull(store.Redeem("tok"), "the expired token was kept and came back to life");
        }

        [TestMethod]
        public void Unknown_and_empty_tokens_are_refused_without_fuss()
        {
            var store = Store();
            Assert.IsNull(store.Redeem("never-issued"));
            Assert.IsNull(store.Redeem(""));
            Assert.IsNull(store.Redeem(null));
        }

        [TestMethod]
        public void Clearing_an_account_removes_its_tokens_and_nobody_elses()
        {
            var store = Store();
            store.Add("a1", 1);
            store.Add("a2", 1);
            store.Add("b1", 2);

            store.RemoveForAccount(1);

            Assert.IsNull(store.Redeem("a1"));
            Assert.IsNull(store.Redeem("a2"));
            Assert.AreEqual(2, store.Redeem("b1"));
        }

        [TestMethod]
        public void Expired_tokens_do_not_accumulate()
        {
            // The store is in memory for the life of the server, so without pruning a busy
            // instance keeps every token anyone was ever issued.
            var store = Store(TimeSpan.FromHours(1));
            for (var i = 0; i < 50; i++) store.Add("old" + i, i);
            Assert.AreEqual(50, store.Count);

            now = now.AddHours(2);
            store.Add("fresh", 999);

            Assert.AreEqual(1, store.Count, "issuing should have swept the expired ones");
            Assert.AreEqual(999, store.Redeem("fresh"));
        }

        [TestMethod]
        public void The_configured_lifetime_is_read_and_nonsense_falls_back()
        {
            Assert.AreEqual(TimeSpan.FromHours(6), SessionTokenStore.ParseLifetime("6"));
            Assert.AreEqual(TimeSpan.FromHours(0.5), SessionTokenStore.ParseLifetime("0.5"));

            // A lifetime of zero or less reads as "turn expiry off" and is not honoured as one -
            // it would make every token dead on arrival, which nobody means by it.
            foreach (var nonsense in new[] { null, "", "   ", "soon", "0", "-1" })
                Assert.AreEqual(SessionTokenStore.DefaultLifetime, SessionTokenStore.ParseLifetime(nonsense),
                    "input: " + (nonsense ?? "(null)"));
        }
    }
}
