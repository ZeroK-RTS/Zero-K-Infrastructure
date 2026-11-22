using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZkData;

namespace Tests.Portable
{
    /// <summary>
    /// The rating constants split out of GlobalConst. They are plain numbers, but the WHR
    /// update multiplies by them on every iteration, so their shape is worth stating.
    /// </summary>
    [TestClass]
    public class RatingConstantsTests
    {
        [TestMethod]
        public void Drift_per_day_shrinks_as_a_player_plays_more()
        {
            // A settled player's rating should wander less between games than a new one's.
            var newcomer = GlobalConst.NaturalRatingVariancePerDay(0);
            var regular = GlobalConst.NaturalRatingVariancePerDay(400);
            var veteran = GlobalConst.NaturalRatingVariancePerDay(5000);

            Assert.IsTrue(newcomer > regular, "a newcomer should drift faster than a regular");
            Assert.IsTrue(regular > veteran, "a regular should drift faster than a veteran");
            Assert.IsTrue(veteran > 0, "drift must stay positive or ratings freeze");
        }

        [TestMethod]
        public void Drift_per_day_halves_at_four_hundred_games()
        {
            // 200000/(games+400) is the published shape: 400 games is the half-way point.
            Assert.AreEqual(GlobalConst.NaturalRatingVariancePerDay(0) / 2f,
                            GlobalConst.NaturalRatingVariancePerDay(400), 1e-9f);
        }

        [TestMethod]
        public void Ladder_window_depends_on_the_deployment_mode()
        {
            // Tests run as Local (see GlobalConstMode.cs), where the window is the wider 90
            // days rather than Live's 30. Pinned so a port that resolves Mode differently -
            // a container without the environment the old box had - fails here.
            Assert.AreEqual(90, GlobalConst.LadderActivityDays);
            Assert.AreEqual(3, GlobalConst.LadderAverageDays);
        }

        [TestMethod]
        public void The_elo_conversion_constant_is_unchanged()
        {
            Assert.AreEqual(0.00003313686f, GlobalConst.EloToNaturalRatingMultiplierSquared, 1e-12f);
            Assert.AreEqual(GlobalConst.EloToNaturalRatingMultiplierSquared * 500,
                            GlobalConst.NaturalRatingVariancePerGame, 1e-12f);
        }
    }
}
