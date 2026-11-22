using System;
using System.Threading;
using ZeroKWeb.Controllers;

namespace ZeroKWeb.Host
{
    /// <summary>
    /// The hourly poll tick Global.asax runs, and the state it keeps to run it hourly.
    ///
    /// On MVC 5 this is two fields and four lines inside PostAuthenticateRequest. It is a class
    /// here for one reason: the harness has to be able to drive it. The gate is an hour, the site
    /// starts the clock at startup, and no check can wait that long - so <see cref="LastRun"/> is
    /// settable and the harness moves it back rather than the interval being shortened for
    /// everybody, or a /Harness endpoint being added that production would also expose.
    ///
    /// ONE deliberate difference from Global.asax, and it is the ordering of two statements:
    ///
    ///     PollController.AutoClosePolls();   // Global.asax runs it...
    ///     lastPollCheck = DateTime.UtcNow;   // ...and claims the hour afterwards
    ///
    /// Claiming afterwards means every request that arrives WHILE AutoClosePolls is running also
    /// passes the gate and starts its own, and if it throws - Global.LobbyApi is null on a host
    /// with no lobby server, and GhostPm is called unguarded - the hour is never claimed at all,
    /// so the next request tries again, and every request after that, forever. This claims the
    /// hour first, with Interlocked, so one request runs the tick and the rest go past. Same
    /// behaviour when nothing goes wrong; a failure costs one request instead of all of them.
    ///
    /// The exception is NOT caught. A tick that throws is a real failure and Global.asax lets it
    /// reach the error handler, which this host reproduces; swallowing it here would make a
    /// broken election look like a working one.
    /// </summary>
    internal static class PollAutoClose
    {
        internal static readonly TimeSpan Interval = TimeSpan.FromMinutes(60);

        // Global.asax: private static DateTime lastPollCheck = DateTime.UtcNow - the clock starts
        // at startup, so the first tick is an hour in, not on the first request.
        private static long lastRunTicks = DateTime.UtcNow.Ticks;

        internal static DateTime LastRun
        {
            get => new DateTime(Interlocked.Read(ref lastRunTicks), DateTimeKind.Utc);
            set => Interlocked.Exchange(ref lastRunTicks, value.Ticks);
        }

        internal static void TickIfDue()
        {
            var now = DateTime.UtcNow;
            var previous = Interlocked.Read(ref lastRunTicks);
            if (now.Subtract(new DateTime(previous, DateTimeKind.Utc)) <= Interval) return;

            // Whoever swaps the value in wins the tick; everyone else read the same old value and
            // loses the exchange, and goes on to serve its request.
            if (Interlocked.CompareExchange(ref lastRunTicks, now.Ticks, previous) != previous) return;

            PollController.AutoClosePolls();
        }
    }
}
