using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared;
using Ratings;
using ZkData;

namespace Tests.Database
{
    /// <summary>
    /// Brings the real rating pipeline up once for the whole run, against the fixture
    /// database, and waits for it to finish computing.
    ///
    /// This drives <see cref="RatingSystems.Init"/> - the same entry point the website
    /// calls at startup - rather than poking at WholeHistoryRating directly, because the
    /// point is to test the pipeline, including the database read and the asynchronous
    /// update, not just the arithmetic. Tests.Portable already covers the arithmetic.
    /// </summary>
    public static class RatingPipeline
    {
        private static readonly object gate = new object();
        private static bool started;
        private static string unavailable;

        /// <summary>The Casual system, once it has finished its first full computation.</summary>
        public static IRatingSystem Casual { get; private set; }

        /// <summary>Accounts present in the fixture, by AccountID.</summary>
        public static Dictionary<int, string> Players { get; private set; }

        /// <summary>The account with the most games - the one ratings converge on best.</summary>
        public static int BusiestPlayer { get; private set; }

        public static void Require()
        {
            lock (gate)
            {
                if (unavailable != null) Assert.Inconclusive(unavailable);
                if (started) return;
                started = true;

                var cs = Environment.GetEnvironmentVariable("ZK_CONNECTION_STRING");
                if (string.IsNullOrEmpty(cs))
                {
                    unavailable = "ZK_CONNECTION_STRING is not set. See db/README.md; run these tests with db/run-db-tests.sh.";
                    Assert.Inconclusive(unavailable);
                }

                try
                {
                    using (var db = new ZkDataContext())
                    {
                        Players = db.Accounts.ToDictionary(x => x.AccountID, x => x.Name);
                        var battles = db.SpringBattles.Count();
                        if (Players.Count == 0 || battles == 0)
                        {
                            unavailable = "The database has no fixture data. Load it with db/load-fixture.sh.";
                            Assert.Inconclusive(unavailable);
                        }
                        Console.WriteLine("  fixture: " + Players.Count + " accounts, " + battles + " battles");
                    }
                }
                catch (SqlException ex)
                {
                    unavailable = "Cannot reach the database: " + ex.Message.Split('\n')[0];
                    Assert.Inconclusive(unavailable);
                }

                // Clear the stored ratings. They are the LIVE pipeline's output, carried
                // along in the fixture; leaving them in place would let GetPlayerRating
                // answer from the database cache and the tests would never touch the
                // computation. The point here is to recompute from the battles.
                using (var db = new ZkDataContext())
                {
                    db.Database.ExecuteSqlCommand("DELETE FROM AccountRatings");
                }

                Console.WriteLine("  starting the rating pipeline...");
                var clock = Stopwatch.StartNew();
                RatingSystems.Init();

                // Init() does its database read on a background task and only then flips
                // Initialized, so the first wait is for the read to finish.
                WaitFor(() => RatingSystems.Initialized, TimeSpan.FromMinutes(3),
                    "RatingSystems.Init did not finish reading battles");

                Casual = RatingSystems.GetRatingSystem(RatingCategory.Casual);

                // Init() calls UpdateRatings itself once the read finishes, and that
                // dispatches the Newton iterations to another task, so the ratings appear
                // later again. ForceRatingsUpdate is on the concrete class rather than the
                // interface; nudging it is harmless if an update is already under way.
                (Casual as WholeHistoryRating)?.ForceRatingsUpdate();

                // Readiness signal: DefaultRating has LastGameDate 0, so a non-zero one
                // means this player's rating came out of the computation rather than the
                // default. Deliberately NOT GetTopPlayers - that only returns players
                // active within GlobalConst.LadderActivityDays, and fixture battles are
                // historical, so it is empty by design. See the ranking test.
                BusiestPlayer = WinLossByAccount().OrderByDescending(x => x.Value.Item2).First().Key;
                WaitFor(() => Casual.GetPlayerRating(BusiestPlayer).LastGameDate > 0, TimeSpan.FromMinutes(3),
                    "the Casual rating system produced no computed ratings");

                // ...and then for the pass to FINISH, which is a different thing and the reason
                // this wait exists at all.
                //
                // WholeHistoryRating.GetTopPlayers reads `count > 200 || !completelyInitialized`
                // and falls back to a database query ordered by LadderElo - which ignores ladder
                // activity entirely. So while the pass is still running, GetTopPlayers(10) returns
                // ten players from a fixture whose battles are all historical, and
                // Players_who_have_not_played_recently_are_rated_but_not_ranked fails saying "no
                // fixture player is recent enough to be ranked".
                //
                // That is a race, and it failed in CI on 2026-09-26 having passed nine minutes
                // earlier on the same code. The wait above is satisfied by a PARTIAL pass: one
                // player having a computed rating says nothing about the other 149 iterations.
                WaitFor(() => FullPassFinished(Casual), TimeSpan.FromMinutes(3),
                    "the WHR pass did not finish, so GetTopPlayers would answer from the database "
                    + "instead of the ladder");

                Console.WriteLine("  pipeline ready in " + clock.Elapsed.TotalSeconds.ToString("0.0") + "s, "
                                  + Casual.GetActivePlayers() + " active players");
            }
        }

        /// <summary>
        /// Whether the Newton pass has run to completion.
        ///
        /// WholeHistoryRating sets <c>completelyInitialized</c> after runIterations(150) and the
        /// ranking pass, and exposes no public equivalent - hence the reflection. Sampling until
        /// the numbers stop moving answers too early: they are perfectly still before the
        /// background task starts.
        ///
        /// Lives here rather than in RatingDump because the pipeline is what every test waits on,
        /// and a second copy would be a second thing to keep true.
        /// </summary>
        internal static bool FullPassFinished(IRatingSystem system)
        {
            var whr = system as WholeHistoryRating;
            if (whr == null) return true;
            var field = typeof(WholeHistoryRating).GetField("completelyInitialized",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null && (bool)field.GetValue(whr);
        }

        private static void WaitFor(Func<bool> condition, TimeSpan timeout, string message)
        {
            var until = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < until)
            {
                bool ok;
                try { ok = condition(); }
                catch { ok = false; }
                if (ok) return;
                System.Threading.Thread.Sleep(250);
            }
            Assert.Fail(message + " within " + timeout.TotalSeconds + "s");
        }

        /// <summary>Win rate straight from the database, so tests do not hardcode fixture internals.</summary>
        public static Dictionary<int, Tuple<int, int>> WinLossByAccount()
        {
            using (var db = new ZkDataContext())
            {
                return db.SpringBattlePlayers
                    .Where(p => !p.IsSpectator)
                    .GroupBy(p => p.AccountID)
                    .ToDictionary(g => g.Key,
                        g => Tuple.Create(g.Count(x => x.IsInVictoryTeam), g.Count()));
            }
        }
    }
}
