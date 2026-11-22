using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ratings;

namespace Tests.Portable
{
    /// <summary>
    /// Day-number conversion. WHR indexes every rating by an integer day since the Unix
    /// epoch, so an off-by-one here shifts a player's whole history.
    /// </summary>
    [TestClass]
    public class RatingDayTests
    {
        [TestMethod]
        public void Day_zero_is_the_unix_epoch()
        {
            Assert.AreEqual(0, RatingSystems.ConvertDateToDays(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), RatingSystems.ConvertDaysToDate(0));
        }

        [TestMethod]
        public void Known_dates_map_to_known_day_numbers()
        {
            Assert.AreEqual(18262, RatingSystems.ConvertDateToDays(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual(1, RatingSystems.ConvertDateToDays(new DateTime(1970, 1, 2, 0, 0, 0, DateTimeKind.Utc)));
        }

        [TestMethod]
        public void The_day_number_truncates_rather_than_rounds()
        {
            // 23:59 is still the same day, not the next one.
            var day = new DateTime(2020, 1, 1, 23, 59, 59, DateTimeKind.Utc);
            Assert.AreEqual(18262, RatingSystems.ConvertDateToDays(day));
        }

        [TestMethod]
        public void Days_round_trip_back_to_midnight_utc()
        {
            foreach (var d in new[] { 0, 1, 18262, 20000 })
            {
                var date = RatingSystems.ConvertDaysToDate(d);
                Assert.AreEqual(DateTimeKind.Utc, date.Kind);
                Assert.AreEqual(d, RatingSystems.ConvertDateToDays(date));
            }
        }

        [TestMethod]
        public void A_DateTime_of_unspecified_kind_is_read_as_local_time_not_utc()
        {
            // Characterization, not endorsement. ConvertDateToDays calls ToUniversalTime(),
            // which treats DateTimeKind.Unspecified as LOCAL time. Values loaded from the
            // database arrive Unspecified, so the day a battle lands on depends on the
            // server's time zone. This test states the behaviour so that a port which
            // changes it - for instance by running in a container on UTC when the old box
            // was not - fails here rather than silently reshuffling rating history.
            var naive = new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
            var asLocal = DateTime.SpecifyKind(naive, DateTimeKind.Local);

            Assert.AreEqual(RatingSystems.ConvertDateToDays(asLocal), RatingSystems.ConvertDateToDays(naive),
                "Unspecified is expected to follow the local-time path");
        }

        [TestMethod]
        public void Day_numbers_increase_with_time()
        {
            var earlier = RatingSystems.ConvertDateToDays(new DateTime(2019, 6, 1, 0, 0, 0, DateTimeKind.Utc));
            var later = RatingSystems.ConvertDateToDays(new DateTime(2019, 6, 2, 0, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(earlier + 1, later);
        }
    }
}
