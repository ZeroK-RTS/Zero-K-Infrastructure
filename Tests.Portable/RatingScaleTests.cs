using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ratings;
using ZkData;

namespace Tests.Portable
{
    /// <summary>
    /// The Elo / natural-rating / gamma scale conversions in PlayerDay. Every rating the
    /// site displays goes through these, so a port that changes them silently rescales
    /// every player.
    /// </summary>
    [TestClass]
    public class RatingScaleTests
    {
        private static PlayerDay NewDay() => new PlayerDay(new Player(1), 0);

        [TestMethod]
        public void Elo_round_trips_through_the_natural_scale()
        {
            foreach (var elo in new[] { -1500f, 0f, 1f, 1500f, 2000f, 4000f })
            {
                var day = NewDay();
                day.SetElo(elo);
                Assert.AreEqual(elo, day.GetElo(), Math.Abs(elo) * 1e-4f + 1e-3f,
                    "Elo " + elo + " did not survive the round trip");
            }
        }

        [TestMethod]
        public void Natural_rating_is_elo_scaled_by_ln10_over_400()
        {
            // r = elo * ln(10)/400 is the WHR scale. Pinned explicitly: it is the constant
            // that ties our numbers to the published algorithm.
            var day = NewDay();
            day.SetElo(400f);
            Assert.AreEqual((float)Math.Log(10), day.GetNaturalRating(), 1e-5f);
        }

        [TestMethod]
        public void Gamma_is_the_exponential_of_the_natural_rating()
        {
            var day = NewDay();
            day.SetGamma(7f);
            Assert.AreEqual((float)Math.Log(7f), day.GetNaturalRating(), 1e-5f);
            Assert.AreEqual(7f, day.GetGamma(), 1e-4f);
        }

        [TestMethod]
        public void Zero_elo_is_gamma_one()
        {
            var day = NewDay();
            day.SetElo(0f);
            Assert.AreEqual(1f, day.GetGamma(), 1e-6f);
        }

        [TestMethod]
        public void Elo_stdev_is_the_variance_converted_off_the_natural_scale()
        {
            var day = NewDay();
            day.naturalRatingVariance = 4f * GlobalConst.EloToNaturalRatingMultiplierSquared;
            Assert.AreEqual(2f, day.GetEloStdev(), 1e-4f);
        }

        [TestMethod]
        public void A_400_elo_lead_is_ten_to_one_in_gamma()
        {
            // The defining property of the Elo scale, and the one a rescaling bug breaks.
            var strong = NewDay();
            var weak = NewDay();
            strong.SetElo(1400f);
            weak.SetElo(1000f);
            Assert.AreEqual(10f, strong.GetGamma() / weak.GetGamma(), 1e-3f);
        }
    }
}
