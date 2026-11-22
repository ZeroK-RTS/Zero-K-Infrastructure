using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared.Imaging;

namespace Tests.Portable
{
    /// <summary>
    /// unitsync's raw pixel buffers, unpacked on .NET 9 - where System.Drawing cannot run, which
    /// is the whole reason this arithmetic was lifted out of UnitSync.cs.
    ///
    /// What these do NOT prove is that the unpacking matches GDI+, because GDI+ is not here. That
    /// is checked exhaustively against the real thing in Tests/UnitSyncPixelTests.cs, which runs
    /// on the Windows CI job. These pin the behaviour that can be pinned anywhere: layout,
    /// bounds, and the values at the ends of the range.
    /// </summary>
    [TestClass]
    public class PixelBufferTests
    {
        [TestMethod]
        public void Black_and_white_survive_the_round_trip_exactly()
        {
            // The ends are where a wrong scaling rule still looks right, so they are necessary
            // but not sufficient - see the exhaustive Windows test.
            var black = PixelBuffers.Rgb565ToRgb24(new byte[] { 0x00, 0x00 }, new Size(1, 1), 2);
            CollectionAssert.AreEqual(new byte[] { 0, 0, 0 }, black);

            var white = PixelBuffers.Rgb565ToRgb24(new byte[] { 0xFF, 0xFF }, new Size(1, 1), 2);
            CollectionAssert.AreEqual(new byte[] { 255, 255, 255 }, white);
        }

        [TestMethod]
        public void The_channels_do_not_get_swapped()
        {
            // 5 red, 6 green, 5 blue. Full red is 0xF800, and a swap would be invisible on grey.
            var red = PixelBuffers.Rgb565ToRgb24(new byte[] { 0x00, 0xF8 }, new Size(1, 1), 2);
            CollectionAssert.AreEqual(new byte[] { 255, 0, 0 }, red);

            var green = PixelBuffers.Rgb565ToRgb24(new byte[] { 0xE0, 0x07 }, new Size(1, 1), 2);
            CollectionAssert.AreEqual(new byte[] { 0, 255, 0 }, green);

            var blue = PixelBuffers.Rgb565ToRgb24(new byte[] { 0x1F, 0x00 }, new Size(1, 1), 2);
            CollectionAssert.AreEqual(new byte[] { 0, 0, 255 }, blue);
        }

        [TestMethod]
        public void Row_padding_in_the_source_is_skipped()
        {
            // unitsync hands back a stride, not a width. A reader that assumed width * 2 would
            // walk diagonally through a padded buffer and still produce an image.
            var source = new byte[]
            {
                0x00, 0xF8, 0x1F, 0x00, 0xAA, 0xAA,   // red, blue, then two bytes of padding
                0xE0, 0x07, 0x00, 0x00, 0xBB, 0xBB,   // green, black, padding
            };

            var pixels = PixelBuffers.Rgb565ToRgb24(source, new Size(2, 2), 6);

            CollectionAssert.AreEqual(new byte[]
            {
                255, 0, 0,   0, 0, 255,
                0, 255, 0,   0, 0, 0,
            }, pixels);
        }

        [TestMethod]
        public void A_greyscale_info_map_goes_into_all_three_channels()
        {
            // The height map's greys ARE the data, so this may not rescale anything.
            var pixels = PixelBuffers.GreyscaleToRgb24(new byte[] { 0, 128, 255, 7 }, new Size(2, 2));

            CollectionAssert.AreEqual(new byte[]
            {
                0, 0, 0,       128, 128, 128,
                255, 255, 255, 7, 7, 7,
            }, pixels);
        }

        [TestMethod]
        public void A_buffer_that_is_too_small_is_refused_rather_than_read_past()
        {
            Assert.ThrowsException<System.ArgumentException>(
                () => PixelBuffers.GreyscaleToRgb24(new byte[] { 1, 2, 3 }, new Size(2, 2)));
        }

        [TestMethod]
        public void A_square_minimap_is_stretched_back_to_the_maps_proportions()
        {
            // unitsync renders every minimap square whatever shape the map is.
            Assert.AreEqual(new Size(1024, 512),
                ImageSizing.MinimapAspectCorrection(new Size(1024, 1024), new Size(2, 1)));

            Assert.AreEqual(new Size(512, 1024),
                ImageSizing.MinimapAspectCorrection(new Size(1024, 1024), new Size(1, 2)));

            Assert.AreEqual(new Size(1024, 1024),
                ImageSizing.MinimapAspectCorrection(new Size(1024, 1024), new Size(1, 1)));
        }
    }
}
