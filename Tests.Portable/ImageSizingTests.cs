using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared.Imaging;

namespace Tests.Portable
{
    /// <summary>
    /// Target sizes for image scaling. A wrong size here distorts an image silently rather
    /// than failing, so this arithmetic is worth pinning before the imaging library is
    /// swapped underneath it.
    /// </summary>
    [TestClass]
    public class ImageSizingTests
    {
        // ---- ScaledToFit: map thumbnails ------------------------------------------

        [TestMethod]
        public void A_landscape_image_fills_the_box_horizontally()
        {
            Assert.AreEqual(new Size(200, 100), ImageSizing.ScaledToFit(2.0, 200));
        }

        [TestMethod]
        public void A_portrait_image_fills_the_box_vertically()
        {
            Assert.AreEqual(new Size(100, 200), ImageSizing.ScaledToFit(0.5, 200));
        }

        [TestMethod]
        public void A_square_image_fills_the_box_exactly()
        {
            Assert.AreEqual(new Size(200, 200), ImageSizing.ScaledToFit(1.0, 200));
        }

        [TestMethod]
        public void Scaling_truncates_rather_than_rounds()
        {
            // (int) cast, as the original did. 200/3 is 66.67, which becomes 66.
            Assert.AreEqual(new Size(200, 66), ImageSizing.ScaledToFit(3.0, 200));
        }

        [TestMethod]
        public void An_unknown_ratio_gets_a_square()
        {
            // Resource.MapSizeRatio is nullable. The original comparison chain fell through
            // to the square branch for null, because both "null > 1" and "null < 1" are
            // false. Pinned so the nullable signature keeps that behaviour.
            Assert.AreEqual(new Size(200, 200), ImageSizing.ScaledToFit(null, 200));
        }

        // ---- ProportionalHeight: news thumbnails ----------------------------------

        [TestMethod]
        public void Thumbnail_height_keeps_the_proportions()
        {
            Assert.AreEqual(60, ImageSizing.ProportionalHeight(240, 120, 120));
            Assert.AreEqual(120, ImageSizing.ProportionalHeight(120, 120, 120));
        }

        [TestMethod]
        public void Thumbnail_height_rounds_to_nearest()
        {
            // Math.Round, unlike ScaledToFit's truncation. 120/7*3 = 51.43 -> 51.
            Assert.AreEqual(51, ImageSizing.ProportionalHeight(7, 3, 120));
        }

        [TestMethod]
        public void A_zero_width_image_does_not_divide_by_zero()
        {
            Assert.AreEqual(0, ImageSizing.ProportionalHeight(0, 100, 120));
        }

        // ---- LegacyToBytesSize: the rule AutoRegistrator uploads through ----------

        [TestMethod]
        public void The_legacy_rule_leaves_a_square_image_alone()
        {
            // Which is why the defect below has gone unnoticed: unitsync hands out square
            // minimaps, and the fault only shows once FixAspectRatio has corrected them.
            Assert.AreEqual(new Size(512, 512), ImageSizing.LegacyToBytesSize(512, 512));
        }

        [TestMethod]
        public void The_legacy_rule_applies_the_aspect_ratio_a_second_time()
        {
            // CHARACTERISATION, NOT APPROVAL. UnitSync.GetMinimap already calls
            // FixAspectRatio, so the image arriving here is in proportion. Applying the
            // ratio again squashes it: a 2:1 image is uploaded as 4:1.
            Assert.AreEqual(new Size(1024, 256), ImageSizing.LegacyToBytesSize(1024, 512));
            Assert.AreEqual(new Size(128, 512), ImageSizing.LegacyToBytesSize(256, 512));
        }

        [TestMethod]
        public void The_legacy_rule_ignores_the_requested_maximum_size()
        {
            // There is no size parameter here because ToBytes never used the one it takes.
            // AutoRegistrator passes 256 as "max size of minimap to be sent to server" and
            // a 1024-wide minimap stays 1024 wide.
            Assert.AreEqual(1024, ImageSizing.LegacyToBytesSize(1024, 1024).Width);
        }

        // ---- BoundedByLongestSide: what it was presumably meant to do -------------

        [TestMethod]
        public void The_intended_rule_keeps_proportions_and_bounds_the_longest_side()
        {
            Assert.AreEqual(new Size(256, 128), ImageSizing.BoundedByLongestSide(1024, 512, 256));
            Assert.AreEqual(new Size(128, 256), ImageSizing.BoundedByLongestSide(512, 1024, 256));
            Assert.AreEqual(new Size(256, 256), ImageSizing.BoundedByLongestSide(1024, 1024, 256));
        }

        [TestMethod]
        public void The_intended_rule_does_not_enlarge_a_small_image()
        {
            Assert.AreEqual(new Size(64, 32), ImageSizing.BoundedByLongestSide(64, 32, 256));
        }

        [TestMethod]
        public void The_intended_rule_never_collapses_a_side_to_zero()
        {
            // An extreme panorama would otherwise round its short side to 0, which is not a
            // valid bitmap.
            Assert.AreEqual(1, ImageSizing.BoundedByLongestSide(10000, 3, 256).Height);
        }
    
        [TestMethod]
        public void A_planet_icon_is_centred_on_its_point()
        {
            // X and Y are fractions of the canvas. Half an icon's offset in either direction
            // still looks like a galaxy, which is why this is pinned rather than eyeballed.
            var placement = ImageSizing.PlanetIconPlacement(0.5, 0.5, new Size(1000, 800), new Size(20, 20), 40, 1);

            Assert.AreEqual(new Rectangle(500 - 20, 400 - 20, 40, 40), placement);
        }

        [TestMethod]
        public void A_planet_icon_keeps_the_aspect_ratio_of_the_icon_file()
        {
            // Width comes from PlanetWarsIconSize; height follows the icon's own proportions.
            var tall = ImageSizing.PlanetIconPlacement(0, 0, new Size(100, 100), new Size(10, 20), 30, 1);

            Assert.AreEqual(30, tall.Width);
            Assert.AreEqual(60, tall.Height);
        }

        [TestMethod]
        public void Zoom_scales_the_icon_but_not_its_position()
        {
            var placement = ImageSizing.PlanetIconPlacement(0.25, 0.75, new Size(400, 400), new Size(10, 10), 20, 2);

            Assert.AreEqual(40, placement.Width);
            Assert.AreEqual(40, placement.Height);
            Assert.AreEqual(100 - 20, placement.X);
            Assert.AreEqual(300 - 20, placement.Y);
        }

        [TestMethod]
        public void Icon_placement_truncates_exactly_where_the_original_did()
        {
            // The width truncates first and the height is derived from the TRUNCATED width, then
            // the halving truncates again. Computing in doubles and rounding once would be
            // defensible and would move icons by a pixel - this pins the port, not the taste.
            var placement = ImageSizing.PlanetIconPlacement(0.333, 0.333, new Size(101, 101), new Size(3, 7), 5.9, 1);

            Assert.AreEqual(5, placement.Width);                    // (int)(5.9 * 1)
            Assert.AreEqual((int)(5 * (7 / 3.0)), placement.Height); // from the truncated width
            Assert.AreEqual((int)(0.333 * 101) - 2, placement.X);
        }

        [TestMethod]
        public void A_legacy_stored_image_is_recognised_by_its_aspect()
        {
            // A 2:1 map: correct is 1024x512, the old rule stored 1024x256.
            Assert.IsTrue(ImageSizing.LooksLikeLegacyToBytes(new Size(1024, 256), 2.0));
            Assert.IsFalse(ImageSizing.LooksLikeLegacyToBytes(new Size(1024, 512), 2.0));

            // And the same the other way up, where the squash is horizontal.
            Assert.IsTrue(ImageSizing.LooksLikeLegacyToBytes(new Size(256, 1024), 0.5));
            Assert.IsFalse(ImageSizing.LooksLikeLegacyToBytes(new Size(512, 1024), 0.5));
        }

        [TestMethod]
        public void A_square_map_is_never_called_legacy()
        {
            // R and R squared are both 1, so there is nothing to tell apart - and nothing wrong
            // with the image either, which is why the defect went unnoticed.
            Assert.IsFalse(ImageSizing.LooksLikeLegacyToBytes(new Size(1024, 1024), 1.0));
            Assert.IsFalse(ImageSizing.LooksLikeLegacyToBytes(new Size(512, 512), 1.0));
        }

        [TestMethod]
        public void An_unknown_ratio_is_not_guessed_at()
        {
            Assert.IsFalse(ImageSizing.LooksLikeLegacyToBytes(new Size(1024, 256), null));
            Assert.IsFalse(ImageSizing.LooksLikeLegacyToBytes(new Size(0, 0), 2.0));
        }

        [TestMethod]
        public void The_fixed_rule_bounds_the_longest_side_and_keeps_proportions()
        {
            // What AutoRegistrator asked for all along by passing ImageSize = 256.
            Assert.AreEqual(new Size(256, 128), ImageSizing.BoundedByLongestSide(1024, 512, 256));
            Assert.AreEqual(new Size(128, 256), ImageSizing.BoundedByLongestSide(512, 1024, 256));
            Assert.AreEqual(new Size(256, 256), ImageSizing.BoundedByLongestSide(1024, 1024, 256));

            // Compare with what was stored instead: the ratio applied twice, at full resolution.
            Assert.AreEqual(new Size(1024, 256), ImageSizing.LegacyToBytesSize(1024, 512));
        }

        [TestMethod]
        public void A_legacy_image_is_stretched_back_along_the_squashed_axis()
        {
            // 2:1 map. Correct was 1024x512; the old rule stored 1024x256. The width survived, so
            // the correction restores the height and keeps every pixel the long axis still has.
            Assert.AreEqual(new Size(1024, 512), ImageSizing.CorrectedFromLegacy(new Size(1024, 256), 2.0));

            // 1:2 map, squashed horizontally instead.
            Assert.AreEqual(new Size(512, 1024), ImageSizing.CorrectedFromLegacy(new Size(256, 1024), 0.5));
        }

        [TestMethod]
        public void Correcting_a_legacy_image_gives_it_the_maps_own_ratio()
        {
            // The property that matters, stated as a property: whatever went in, what comes out
            // has the map's aspect - and is then no longer recognised as legacy.
            foreach (var ratio in new[] { 1.25, 1.5, 2.0, 3.0, 0.8, 0.5, 0.333 })
            {
                var legacy = ratio > 1
                    ? new Size(1024, (int)(1024 / (ratio * ratio)))
                    : new Size((int)(1024 * ratio * ratio), 1024);

                var corrected = ImageSizing.CorrectedFromLegacy(legacy, ratio);

                Assert.IsTrue(ImageSizing.LooksLikeLegacyToBytes(legacy, ratio),
                    "the fixture for ratio " + ratio + " should look legacy to begin with");
                Assert.IsFalse(ImageSizing.LooksLikeLegacyToBytes(corrected, ratio),
                    "after correction it should not, or the backfill would not be idempotent");
                Assert.AreEqual(ratio, (double)corrected.Width / corrected.Height, 0.02,
                    "corrected image should carry the map's ratio");
            }
        }
}
}
