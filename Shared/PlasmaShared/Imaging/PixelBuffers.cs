using System;
using System.Drawing;

namespace PlasmaShared.Imaging
{
    /// <summary>
    /// The pixel unpacking unitsync needs, with no imaging library in it.
    ///
    /// unitsync hands back raw buffers - a 16-bit RGB565 minimap and 8-bit info maps - and
    /// UnitSync.cs turned them into Bitmaps, which is the last thing in Shared/PlasmaShared
    /// that cannot compile on .NET 9. The conversion itself is arithmetic, so it lives here,
    /// is tested on Linux, and is the same code on both stacks.
    ///
    /// Everything returns tightly packed 24-bit RGB - three bytes per pixel, no row padding -
    /// because that is what an encoder wants and what a test can read.
    /// </summary>
    public static class PixelBuffers
    {
        /// <summary>
        /// unitsync's minimap: 16 bits per pixel, 5 red, 6 green, 5 blue, little-endian.
        ///
        /// **The scaling is the part that matters.** Five bits of red have to become eight, and
        /// the choice between <c>x * 255 / 31</c> and bit replication <c>(x &lt;&lt; 3) | (x &gt;&gt; 2)</c>
        /// changes every pixel slightly. This uses bit replication, which is what GDI+ does -
        /// and that is not taken on trust: Tests/UnitSyncPixelTests.cs runs on the Windows CI
        /// job and compares this against GDI+ for **all 65,536** possible values, which is the
        /// only oracle available for it in this repository.
        /// </summary>
        public static byte[] Rgb565ToRgb24(byte[] source, Size size, int stride)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (size.Width < 0 || size.Height < 0) throw new ArgumentException("negative size", "size");

            var target = new byte[size.Width * size.Height * 3];
            for (var y = 0; y < size.Height; y++)
            {
                var row = y * stride;
                for (var x = 0; x < size.Width; x++)
                {
                    var at = row + x * 2;
                    int value = source[at] | (source[at + 1] << 8);

                    var r = (value >> 11) & 0x1F;
                    var g = (value >> 5) & 0x3F;
                    var b = value & 0x1F;

                    var to = (y * size.Width + x) * 3;
                    target[to] = (byte)((r << 3) | (r >> 2));
                    target[to + 1] = (byte)((g << 2) | (g >> 4));
                    target[to + 2] = (byte)((b << 3) | (b >> 2));
                }
            }
            return target;
        }

        /// <summary>
        /// unitsync's info maps - the height map and the metal map - are one byte per pixel, and
        /// GetInfoMap wrote each one into all three channels. Grey, in other words, and the
        /// height map's greys ARE the data, so nothing here may rescale them.
        /// </summary>
        public static byte[] GreyscaleToRgb24(byte[] source, Size size)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (source.Length < size.Width * size.Height)
                throw new ArgumentException("buffer is smaller than " + size.Width + "x" + size.Height, "source");

            var target = new byte[size.Width * size.Height * 3];
            for (var i = 0; i < size.Width * size.Height; i++)
            {
                var v = source[i];
                target[i * 3] = v;
                target[i * 3 + 1] = v;
                target[i * 3 + 2] = v;
            }
            return target;
        }
    
        /// <summary>
        /// Swaps the red and blue channels of tightly packed 24-bit pixels, in place.
        ///
        /// **GDI+'s Format24bppRgb is BGR in memory**, despite the name, while everything here and
        /// every encoder speaks RGB. Writing an RGB buffer into a Format24bppRgb bitmap without
        /// this produces a picture that is obviously wrong only if something in it was supposed to
        /// be red - and unitsync's height and metal maps are grey, where the mistake is invisible.
        /// So it is a named, tested step rather than a loop index somewhere.
        /// </summary>
        public static byte[] SwapRedAndBlue(byte[] pixels)
        {
            if (pixels == null) throw new ArgumentNullException("pixels");
            if (pixels.Length % 3 != 0) throw new ArgumentException("not whole 24-bit pixels", "pixels");

            for (var i = 0; i < pixels.Length; i += 3)
            {
                var red = pixels[i];
                pixels[i] = pixels[i + 2];
                pixels[i + 2] = red;
            }
            return pixels;
        }
}
}
