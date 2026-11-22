using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroKWeb;

namespace Tests
{
    /// <summary>
    /// The site's rate limiter. It has run in production for years and was never ported, so the
    /// .NET 9 host had no rate limiting at all; these pin what it does while it is being moved,
    /// which is the point of writing them against the arithmetic rather than against HTTP.
    ///
    /// The clock is driven by hand. The window is five seconds, and a test that proves the second
    /// limit by sleeping through it is slow and occasionally wrong - and worse, a test that slept
    /// too little would pass against a limiter that counted nothing.
    /// </summary>
    [TestClass]
    public class DosProtectorTests
    {
        private DateTime now = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

        private DosProtector Protector() => new DosProtector(() => now);

        [TestMethod]
        public void AnIdleAddressIsAllowed()
        {
            Assert.IsTrue(Protector().CanQuery("10.0.0.1"));
        }

        [TestMethod]
        public void NoAddressIsAllowed()
        {
            // Both stacks can hand this a null: UserHostAddress on MVC 5 and RemoteIpAddress on
            // ASP.NET Core. Refusing a request because its address is unknown would be a worse
            // failure than serving it.
            var protector = Protector();
            for (var i = 0; i < 100; i++) protector.RequestStart(null);
            Assert.IsTrue(protector.CanQuery(null));
        }

        [TestMethod]
        public void MoreThanFifteenInFlightIsRefused()
        {
            var protector = Protector();
            for (var i = 0; i < DosProtector.MaxParallel; i++) protector.RequestStart("10.0.0.1");

            // Exactly at the limit is still allowed - the original says `> 15`, not `>= 15`.
            Assert.IsTrue(protector.CanQuery("10.0.0.1"), "fifteen in flight is the limit, not past it");

            protector.RequestStart("10.0.0.1");
            Assert.IsFalse(protector.CanQuery("10.0.0.1"), "sixteen in flight is past it");
        }

        [TestMethod]
        public void TheLimitIsPerAddress()
        {
            var protector = Protector();
            for (var i = 0; i < DosProtector.MaxParallel + 1; i++) protector.RequestStart("10.0.0.1");

            Assert.IsFalse(protector.CanQuery("10.0.0.1"));
            Assert.IsTrue(protector.CanQuery("10.0.0.2"), "one address must not shut out another");
        }

        [TestMethod]
        public void SecondsInsideTheWindowAreCounted()
        {
            var protector = Protector();

            // Three requests of four seconds each, finished: twelve seconds of request time, which
            // is past the eleven the window allows - except that only the part INSIDE the window
            // counts, and the window is five seconds long.
            for (var i = 0; i < 3; i++)
            {
                var id = protector.RequestStart("10.0.0.1");
                now = now.AddSeconds(4);
                protector.RequestEnd(id);
            }

            // Twelve seconds of work, but it happened over twelve seconds, so at most five of it
            // can be inside a five-second window.
            Assert.IsTrue(protector.CanQuery("10.0.0.1"), "work spread over time is not a flood");
        }

        [TestMethod]
        public void ConcurrentSlowRequestsExhaustTheWindow()
        {
            var protector = Protector();

            // Four requests running at once for three seconds: twelve seconds of request time
            // inside a three-second span, all of it in the window.
            var ids = new long[4];
            for (var i = 0; i < ids.Length; i++) ids[i] = protector.RequestStart("10.0.0.1");
            now = now.AddSeconds(3);
            foreach (var id in ids) protector.RequestEnd(id);

            Assert.IsFalse(protector.CanQuery("10.0.0.1"),
                           "twelve request-seconds inside a five-second window is past the limit");

            // And it recovers: move past the window and the same address is served again.
            now = now.Add(DosProtector.Window).AddSeconds(1);
            Assert.IsTrue(protector.CanQuery("10.0.0.1"), "the window has to slide, or a burst is a ban");
        }

        [TestMethod]
        public void AnUnstartedRequestEndsHarmlessly()
        {
            // An exempt request is never started, so nothing ends it. On MVC 5 that was a null out
            // of HttpContext.Items; EndRequest ran for those too.
            Protector().RequestEnd(null);
        }

        [TestMethod]
        public void FinishedRequestsAreForgotten()
        {
            var protector = Protector();
            var id = protector.RequestStart("10.0.0.1");
            protector.RequestEnd(id);

            // RequestEnd drops what has fallen out of the window, and the sweep is what keeps this
            // from being a dictionary that grows for the life of the process.
            now = now.Add(DosProtector.Window).AddSeconds(1);
            protector.RequestEnd(protector.RequestStart("10.0.0.2"));

            Assert.AreEqual(1, protector.Tracked, "the old entry should have been swept");
        }
    }
}
