using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared.Imaging;

namespace Tests
{
    /// <summary>
    /// Compares PixelBuffers against GDI+ itself.
    ///
    /// **This is the only oracle in the repository for the one thing unitsync's port cannot
    /// otherwise verify.** UnitSync.cs handed unitsync's raw RGB565 minimap straight to
    /// System.Drawing, which unpacked five bits of red into eight by its own rule. The port has
    /// to reproduce that rule, and the difference between the plausible choices -
    /// <c>x * 255 / 31</c> versus bit replication - is a shade on every pixel of every minimap:
    /// visible if you look for it, invisible if you do not, and impossible to check on Linux
    /// because System.Drawing.Common throws there.
    ///
    /// So it is checked where GDI+ exists - the Windows CI job - and carries TestCategory("Basic")
    /// because that job filters on it. It is also the first thing in this project to use that
    /// job for something only Windows can answer, rather than as a slower second compile.
    ///
    /// The portable behaviour of the same code is tested on .NET 9 in Tests.Portable.
    /// </summary>
    [TestClass]
    public class UnitSyncPixelTests
    {
        /// <summary>
        /// Every one of the 65,536 RGB565 values, unpacked by GDI+ and by us.
        ///
        /// Exhaustive rather than sampled: the two candidate rules agree on 0 and 31 and differ
        /// in the middle, so any test that checked only the ends would pass on the wrong one.
        /// </summary>
        [TestMethod]
        [TestCategory("Basic")]
        public void Rgb565_unpacking_matches_GDI_for_every_possible_value()
        {
            const int width = 256;
            const int height = 256;

            var source = new byte[width * height * 2];
            for (var value = 0; value < 65536; value++)
            {
                source[value * 2] = (byte)(value & 0xFF);
                source[value * 2 + 1] = (byte)(value >> 8);
            }

            var ours = PixelBuffers.Rgb565ToRgb24(source, new Size(width, height), width * 2);

            var handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try
            {
                using (var gdi = new Bitmap(width, height, width * 2, PixelFormat.Format16bppRgb565,
                                            Marshal.UnsafeAddrOfPinnedArrayElement(source, 0)))
                {
                    for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                    {
                        var expected = gdi.GetPixel(x, y);
                        var at = (y * width + x) * 3;

                        Assert.AreEqual(expected.R, ours[at],
                            string.Format("red differs at {0},{1}", x, y));
                        Assert.AreEqual(expected.G, ours[at + 1],
                            string.Format("green differs at {0},{1}", x, y));
                        Assert.AreEqual(expected.B, ours[at + 2],
                            string.Format("blue differs at {0},{1}", x, y));
                    }
                }
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// The other half of the layout problem: GDI+'s Format24bppRgb is BGR in memory, so a
        /// buffer written straight in comes back with red and blue swapped - and unitsync's height
        /// and metal maps are grey, where that mistake is completely invisible.
        /// </summary>
        [TestMethod]
        [TestCategory("Basic")]
        public void An_RGB24_buffer_becomes_a_bitmap_with_its_colours_the_right_way_round()
        {
            var pixels = new byte[]
            {
                255, 0, 0,    0, 255, 0,      // red,   green
                0, 0, 255,    10, 20, 30,     // blue,  something asymmetric
            };

            using (var bitmap = GdiPixelBridge.FromRgb24(pixels, new Size(2, 2)))
            {
                Assert.AreEqual(Color.FromArgb(255, 255, 0, 0), bitmap.GetPixel(0, 0));
                Assert.AreEqual(Color.FromArgb(255, 0, 255, 0), bitmap.GetPixel(1, 0));
                Assert.AreEqual(Color.FromArgb(255, 0, 0, 255), bitmap.GetPixel(0, 1));
                Assert.AreEqual(Color.FromArgb(255, 10, 20, 30), bitmap.GetPixel(1, 1));
            }
        }

        /// <summary>
        /// Rows are padded to a four-byte boundary by GDI+, and a 3-pixel row is 9 bytes - so this
        /// is a width at which a straight copy shears the image diagonally.
        /// </summary>
        [TestMethod]
        [TestCategory("Basic")]
        public void Row_padding_is_handled_at_a_width_that_needs_it()
        {
            var pixels = new byte[3 * 3 * 3];
            for (var i = 0; i < 9; i++) pixels[i * 3] = (byte)(i * 20);   // varying red per pixel

            using (var bitmap = GdiPixelBridge.FromRgb24(pixels, new Size(3, 3)))
                for (var y = 0; y < 3; y++)
                for (var x = 0; x < 3; x++)
                    Assert.AreEqual((byte)((y * 3 + x) * 20), bitmap.GetPixel(x, y).R,
                        string.Format("pixel {0},{1} came from the wrong row", x, y));
        }
    }
}
