using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ratings;
using ZkData;

namespace Tests.Database
{
    /// <summary>
    /// The Whole History Rating pipeline, end to end, against the committed fixture:
    /// read 150 real battles out of the database, rate 30 players, and check the results
    /// are the shape the site depends on.
    ///
    /// These are properties, not pinned numbers. WHR's output depends on iteration count
    /// and ordering, so asserting exact Elo would pin an implementation detail and break
    /// on any tuning. What must not change is the ordering, the convergence and the
    /// bounds.
    /// </summary>
    [TestClass]
    public class WhrPipelineTests
    {
        [TestInitialize]
        public void SetUp()
        {
            RatingPipeline.Require();
        }

        [TestMethod]
        public void Every_player_who_fought_gets_a_rating()
        {
            var played = RatingPipeline.WinLossByAccount();
            Assert.IsTrue(played.Count > 0, "the fixture has no non-spectating players");

            foreach (var accountId in played.Keys)
            {
                var rating = RatingPipeline.Casual.GetPlayerRating(accountId);
                Assert.IsNotNull(rating, "no rating for account " + accountId);
                Assert.IsFalse(float.IsNaN(rating.RealElo), "NaN elo for account " + accountId);
                Assert.IsFalse(float.IsInfinity(rating.RealElo), "infinite elo for account " + accountId);
            }
        }

        [TestMethod]
        public void Ratings_stay_inside_a_plausible_range()
        {
            // Not a tight bound - just wide enough to catch a scale or sign error, which is
            // the failure mode a port would introduce.
            foreach (var accountId in RatingPipeline.WinLossByAccount().Keys)
            {
                var elo = RatingPipeline.Casual.GetPlayerRating(accountId).RealElo;
                Assert.IsTrue(elo > 0 && elo < 5000, "elo out of range for account " + accountId + ": " + elo);
            }
        }

        [TestMethod]
        public void The_best_record_in_the_fixture_outranks_the_worst()
        {
            // Both ends are computed from the database rather than hardcoded, so the test
            // survives a regenerated fixture.
            var records = RatingPipeline.WinLossByAccount()
                .Where(x => x.Value.Item2 >= 10)
                .ToList();
            Assert.IsTrue(records.Count >= 2, "need at least two players with 10+ games; have " + records.Count);

            var best = records.OrderByDescending(x => (double)x.Value.Item1 / x.Value.Item2).First();
            var worst = records.OrderBy(x => (double)x.Value.Item1 / x.Value.Item2).First();

            var bestElo = RatingPipeline.Casual.GetPlayerRating(best.Key).RealElo;
            var worstElo = RatingPipeline.Casual.GetPlayerRating(worst.Key).RealElo;

            Console.WriteLine(string.Format("        best {0} {1}/{2} -> {3:0}, worst {4} {5}/{6} -> {7:0}",
                RatingPipeline.Players[best.Key], best.Value.Item1, best.Value.Item2, bestElo,
                RatingPipeline.Players[worst.Key], worst.Value.Item1, worst.Value.Item2, worstElo));

            Assert.IsTrue(bestElo > worstElo,
                "the best record should rate above the worst: " + bestElo + " vs " + worstElo);
        }

        [TestMethod]
        public void Win_rate_and_rating_agree_on_the_broad_ordering()
        {
            // Rank correlation rather than a per-player claim: WHR weighs opponent strength,
            // so an individual player can legitimately sit against their raw win rate.
            var records = RatingPipeline.WinLossByAccount().Where(x => x.Value.Item2 >= 10).ToList();
            var byWinRate = records.OrderBy(x => (double)x.Value.Item1 / x.Value.Item2).Select(x => x.Key).ToList();
            var byElo = records.OrderBy(x => RatingPipeline.Casual.GetPlayerRating(x.Key).RealElo).Select(x => x.Key).ToList();

            int concordant = 0, total = 0;
            for (var i = 0; i < byWinRate.Count; i++)
                for (var j = i + 1; j < byWinRate.Count; j++)
                {
                    total++;
                    if (byElo.IndexOf(byWinRate[i]) < byElo.IndexOf(byWinRate[j])) concordant++;
                }

            var agreement = (double)concordant / total;
            Console.WriteLine("        rank agreement: " + agreement.ToString("0.00") + " over " + total + " pairs");
            Assert.IsTrue(agreement > 0.6,
                "rating order should broadly follow win rate; agreement was " + agreement.ToString("0.00"));
        }

        [TestMethod]
        public void A_player_with_more_games_is_known_more_precisely()
        {
            var records = RatingPipeline.WinLossByAccount().Where(x => x.Value.Item2 >= 2).ToList();
            var most = records.OrderByDescending(x => x.Value.Item2).First();
            var least = records.OrderBy(x => x.Value.Item2).First();
            Assert.AreNotEqual(most.Key, least.Key, "need players with differing game counts");

            var mostStdev = RatingPipeline.Casual.GetPlayerRating(most.Key).EloStdev;
            var leastStdev = RatingPipeline.Casual.GetPlayerRating(least.Key).EloStdev;

            Console.WriteLine(string.Format("        {0} games -> stdev {1:0.0};  {2} games -> stdev {3:0.0}",
                most.Value.Item2, mostStdev, least.Value.Item2, leastStdev));

            Assert.IsTrue(mostStdev < leastStdev,
                "more games should mean lower uncertainty: " + mostStdev + " vs " + leastStdev);
        }

        [TestMethod]
        public void Predicted_win_chance_favours_the_stronger_side()
        {
            using (var db = new ZkDataContext())
            {
                var records = RatingPipeline.WinLossByAccount().Where(x => x.Value.Item2 >= 10).ToList();
                var strongId = records.OrderByDescending(x => RatingPipeline.Casual.GetPlayerRating(x.Key).RealElo).First().Key;
                var weakId = records.OrderBy(x => RatingPipeline.Casual.GetPlayerRating(x.Key).RealElo).First().Key;

                var strong = db.Accounts.Single(x => x.AccountID == strongId);
                var weak = db.Accounts.Single(x => x.AccountID == weakId);

                var chances = RatingPipeline.Casual.PredictOutcome(
                    new List<List<Account>> { new List<Account> { strong }, new List<Account> { weak } },
                    DateTime.UtcNow);

                Assert.AreEqual(2, chances.Count, "expected one chance per team");
                Console.WriteLine(string.Format("        strong {0:0.00} vs weak {1:0.00}", chances[0], chances[1]));
                Assert.IsTrue(chances[0] > chances[1],
                    "the stronger player should be favoured: " + chances[0] + " vs " + chances[1]);
                Assert.AreEqual(1.0f, chances.Sum(), 0.01f, "win chances should sum to 1");
            }
        }

        [TestMethod]
        public void Players_who_have_not_played_recently_are_rated_but_not_ranked()
        {
            // Rating and ranking are separate, and this is where the difference shows.
            // UpdateLadderRatings only sets onLadder for players with a game inside
            // GlobalConst.LadderActivityDays; the fixture's battles are historical, so
            // everyone here is rated and nobody is ranked.
            //
            // Stated as a test rather than worked around, because it is the reason
            // GetTopPlayers is empty against this data - which would otherwise look like a
            // broken pipeline to whoever reads this next.
            var id = RatingPipeline.BusiestPlayer;
            var rating = RatingPipeline.Casual.GetPlayerRating(id);

            // Asserted first, and separately, because this test's premise depends on it: until the
            // pass finishes, GetTopPlayers answers from the database ordered by LadderElo and
            // ignores ladder activity, so it returns ten players from a historical fixture. That
            // happened in CI on 2026-09-26, and the message it failed with - "no fixture player is
            // recent enough to be ranked" - pointed at the data rather than at the race.
            Assert.IsTrue(RatingPipeline.FullPassFinished(RatingPipeline.Casual),
                "the WHR pass has not finished, so GetTopPlayers is answering from the database "
                + "and the assertions below are about the wrong code path");

            Assert.IsTrue(rating.LastGameDate > 0, "the busiest player should have a computed rating");
            Assert.IsFalse(rating.Ranked, "a player last seen years ago should not be on the ladder");
            // The claim is "nobody here is ranked", and this asserts exactly that - for every
            // player the ladder hands back, rather than by counting them.
            //
            // Counting was wrong, and wrong in a way that passed most of the time. GetTopPlayers
            // keeps laddersCache in a FIELD: the branch that runs before the pass finishes fills
            // it from the database ordered by LadderElo, which ignores ladder activity, and the
            // top-up guard below it is `laddersCache.Count < count` - false at exactly count. So
            // one call during the pass leaves ten database rows in the cache permanently, and
            // asserting zero fails.
            //
            // That fallback is deliberate; the comment in WholeHistoryRating defends it, so "the
            // list is empty" was never something the code promised. What it does promise is that
            // a player last seen years ago is not ranked, which is path-independent.
            foreach (var player in RatingPipeline.Casual.GetTopPlayers(10))
                Assert.IsFalse(RatingPipeline.Casual.GetPlayerRating(player.AccountID).Ranked,
                    "the ladder returned " + player.Name + ", whose last game is historical - "
                    + "GetTopPlayers may fall back to a database ordering, but nothing in this "
                    + "fixture should come back RANKED");
        }

        [TestMethod]
        public void Rating_history_is_recorded_for_the_days_a_player_fought()
        {
            var busiest = RatingPipeline.WinLossByAccount().OrderByDescending(x => x.Value.Item2).First();
            var history = RatingPipeline.Casual.GetPlayerRatingHistory(busiest.Key);

            Assert.IsNotNull(history, "no history for the busiest player");
            Assert.IsTrue(history.Count > 1,
                "a player with " + busiest.Value.Item2 + " games should have several rating days, got " + history.Count);
            foreach (var point in history)
                Assert.IsFalse(float.IsNaN(point.Value), "NaN in rating history at " + point.Key);
        }

        [TestMethod]
        public void Reading_a_rating_twice_gives_the_same_answer()
        {
            // Guards against the caches in WholeHistoryRating handing out different values
            // on successive reads, which would make any downstream display flicker.
            var id = RatingPipeline.WinLossByAccount().OrderByDescending(x => x.Value.Item2).First().Key;
            var first = RatingPipeline.Casual.GetPlayerRating(id).RealElo;
            var second = RatingPipeline.Casual.GetPlayerRating(id).RealElo;
            Assert.AreEqual(first, second, 0f, "the same account gave two different ratings");
        }

        /// <summary>
        /// The ladder is served from the ladder once the pass has finished, not from the database
        /// ordering the degraded path produces.
        ///
        /// This is the regression test for laddersCache surviving the transition. WholeHistoryRating
        /// calls GetTopPlayers itself while completelyInitialized is still false, which fills the
        /// cache from a query ordered by LadderElo that ignores ladder activity; UpdateRankings has
        /// already cleared the cache by then, so that call was the last writer and nothing cleared
        /// it again until the next ladder pass.
        ///
        /// **How much this test can prove, honestly.** The pipeline calls ForceRatingsUpdate after
        /// Init, and that second pass runs UpdateRankings again - this time with the flag already
        /// set - which rebuilds the cache from the ladder and hides the bug. So in the default
        /// configuration this test passes either way, and it was only shown to catch the defect by
        /// suppressing that second update: without the fix it then fails with Expected:<0>
        /// Actual:<10>, the same signature CI produced, and with the fix it passes.
        ///
        /// That is also why the fix belongs in the production code rather than in a wait here: what
        /// heals it today is a second pass that happens to follow, not anything guaranteeing it.
        /// </summary>
        [TestMethod]
        public void The_ladder_is_rebuilt_once_the_pass_has_finished()
        {
            var ladder = RatingPipeline.Casual.GetTopPlayers(10);

            Assert.AreEqual(0, ladder.Count,
                "every battle in the fixture is historical, so the ladder is empty - a non-empty "
                + "answer here is the database ordering, which ignores ladder activity");
        }

        /// <summary>
        /// A request for more than 200 players does not disturb the ladder.
        ///
        /// It is answered from the database ordered by LadderElo whatever the pass has done, and
        /// it used to write that answer into the same field the ladder is cached in - so the next
        /// ordinary call found the cache no smaller than the count it wanted and served a database
        /// ordering instead of the ladder.
        ///
        /// Deterministic, unlike the timing-dependent version of this: one call, then the other.
        /// </summary>
        [TestMethod]
        public void A_large_request_does_not_replace_the_ladder()
        {
            var large = RatingPipeline.Casual.GetTopPlayers(250);
            Assert.IsTrue(large.Count > 0, "the fixture has accounts, so the database answer is not empty");

            Assert.AreEqual(0, RatingPipeline.Casual.GetTopPlayers(10).Count,
                "the ladder is still empty - every battle in the fixture is historical - and the "
                + "large request above must not have left its database ordering behind");
        }
    }
}