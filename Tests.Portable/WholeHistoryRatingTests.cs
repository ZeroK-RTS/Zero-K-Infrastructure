using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ratings;

namespace Tests.Portable
{
    /// <summary>
    /// The WHR update itself: does a player who wins go up, does the model stay stable, and
    /// do team sizes weigh the way they are supposed to. These are the properties the port
    /// must preserve; the exact float values are not pinned, because they legitimately
    /// depend on iteration order and are not a contract.
    /// </summary>
    [TestClass]
    public class WholeHistoryRatingTests
    {
        private static int nextGameId = 1;

        /// <summary>One game of winners vs a single losing team, on the given day.</summary>
        private static Game Play(ICollection<Player> winners, ICollection<Player> losers, int day)
        {
            var game = new Game(winners, new List<ICollection<Player>> { losers }, day, nextGameId++);
            foreach (var p in winners) p.AddGame(game);
            foreach (var p in losers) p.AddGame(game);
            return game;
        }

        private static void Converge(IEnumerable<Player> players, int iterations = 50)
        {
            for (var i = 0; i < iterations; i++)
                foreach (var p in players)
                    p.RunOneNewtonIteration(true);
        }

        private static float Elo(Player p) => p.days[p.days.Count - 1].GetElo();

        [TestMethod]
        public void A_player_who_only_wins_ends_above_a_player_who_only_loses()
        {
            var winner = new Player(1);
            var loser = new Player(2);
            var both = new[] { winner, loser };

            for (var day = 0; day < 10; day++)
                Play(new[] { winner }, new[] { loser }, day);

            Converge(both);

            Assert.IsTrue(Elo(winner) > Elo(loser),
                "winner " + Elo(winner) + " should outrank loser " + Elo(loser));
        }

        [TestMethod]
        public void The_gap_widens_with_more_one_sided_results()
        {
            float GapAfter(int games)
            {
                var a = new Player(1);
                var b = new Player(2);
                for (var day = 0; day < games; day++) Play(new[] { a }, new[] { b }, day);
                Converge(new[] { a, b });
                return Elo(a) - Elo(b);
            }

            Assert.IsTrue(GapAfter(20) > GapAfter(3),
                "twenty straight wins should separate two players further than three");
        }

        [TestMethod]
        public void Ratings_stay_finite_through_a_long_one_sided_streak()
        {
            // The update is exponential in the natural scale; an unbounded streak is where
            // it would overflow if a port changed the float handling.
            var a = new Player(1);
            var b = new Player(2);
            for (var day = 0; day < 100; day++) Play(new[] { a }, new[] { b }, day);
            Converge(new[] { a, b }, 100);

            foreach (var p in new[] { a, b })
                foreach (var d in p.days)
                {
                    Assert.IsFalse(float.IsNaN(d.GetElo()), "NaN elo");
                    Assert.IsFalse(float.IsInfinity(d.GetElo()), "infinite elo");
                    Assert.IsFalse(float.IsNaN(d.GetEloStdev()), "NaN stdev");
                }
        }

        [TestMethod]
        public void Trading_wins_evenly_leaves_two_players_level()
        {
            var a = new Player(1);
            var b = new Player(2);
            for (var day = 0; day < 20; day++)
            {
                if (day % 2 == 0) Play(new[] { a }, new[] { b }, day);
                else Play(new[] { b }, new[] { a }, day);
            }
            Converge(new[] { a, b });

            Assert.AreEqual(Elo(a), Elo(b), 25f,
                "an even record should leave the two within a quarter of a class of each other");
        }

        [TestMethod]
        public void Uncertainty_falls_as_games_are_played()
        {
            float StdevAfter(int games)
            {
                var a = new Player(1);
                var b = new Player(2);
                for (var day = 0; day < games; day++)
                {
                    if (day % 2 == 0) Play(new[] { a }, new[] { b }, day);
                    else Play(new[] { b }, new[] { a }, day);
                }
                Converge(new[] { a, b });
                return a.days[a.days.Count - 1].GetEloStdev();
            }

            Assert.IsTrue(StdevAfter(30) < StdevAfter(2),
                "a player with thirty games should be known better than one with two");
        }

        [TestMethod]
        public void Each_member_of_a_team_carries_an_equal_share()
        {
            var w1 = new Player(1);
            var w2 = new Player(2);
            var l1 = new Player(3);
            var game = Play(new[] { w1, w2 }, new[] { l1 }, 0);

            Assert.AreEqual(0.5f, game.GetPlayerWeight(w1), 1e-6f);
            Assert.AreEqual(0.5f, game.GetPlayerWeight(w2), 1e-6f);
            Assert.AreEqual(1.0f, game.GetPlayerWeight(l1), 1e-6f);
        }

        [TestMethod]
        public void Beating_a_bigger_team_is_worth_more_than_beating_a_smaller_one()
        {
            float EloAfterBeating(int opponentCount)
            {
                var hero = new Player(1);
                var opponents = new List<Player>();
                for (var i = 0; i < opponentCount; i++) opponents.Add(new Player(100 + i));

                var everyone = new List<Player> { hero };
                everyone.AddRange(opponents);

                for (var day = 0; day < 10; day++) Play(new[] { hero }, opponents, day);
                Converge(everyone);
                return Elo(hero);
            }

            Assert.IsTrue(EloAfterBeating(4) > EloAfterBeating(1),
                "soloing four players should rate above beating one");
        }
    }
}
