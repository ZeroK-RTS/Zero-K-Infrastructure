using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared.Imaging;
using SixLabors.ImageSharp;          // Save(Stream, encoder) is an extension method
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Size = System.Drawing.Size;    // the seam speaks System.Drawing.Size
using Six = SixLabors.ImageSharp;

namespace Tests.Portable
{
    /// <summary>
    /// The ImageSharp implementation of the imaging seam, exercised on .NET 9, on Linux -
    /// where System.Drawing.Common cannot run at all.
    ///
    /// That is the point of these tests. The port's imaging story is a claim until
    /// something decodes and re-encodes a real image off Windows; this does, with real
    /// bytes and real files, and the assertions are about what the server depends on:
    /// dimensions, that the output is a readable image, and that the format follows the
    /// file extension the way the old code did.
    /// </summary>
    [TestClass]
    public class ImageSharpProcessorTests
    {
        private readonly IImageProcessor processor = new ImageSharpImageProcessor();
        private string dir;

        [TestInitialize]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "zk-imaging-" + Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
        }

        /// <summary>A PNG of the given size, with a recognisable non-uniform pattern.</summary>
        private static byte[] MakeImage(int width, int height)
        {
            using (var image = new Six.Image<Rgba32>(width, height))
            using (var buffer = new MemoryStream())
            {
                image.Mutate(x => x.BackgroundColor(Six.Color.CornflowerBlue));
                image.Save(buffer, new Six.Formats.Png.PngEncoder());
                return buffer.ToArray();
            }
        }

        [TestMethod]
        public void Measure_reports_the_real_dimensions()
        {
            Assert.AreEqual(new Size(640, 480), processor.Measure(MakeImage(640, 480)));
            Assert.AreEqual(new Size(1, 1), processor.Measure(MakeImage(1, 1)));
        }

        [TestMethod]
        public void Save_writes_a_file_that_reads_back_at_the_same_size()
        {
            var path = Path.Combine(dir, "original.png");
            processor.Save(MakeImage(320, 200), path);

            Assert.IsTrue(File.Exists(path), "nothing was written");
            Assert.AreEqual(new Size(320, 200), processor.Measure(File.ReadAllBytes(path)));
        }

        [TestMethod]
        public void SaveResized_writes_the_requested_size()
        {
            var path = Path.Combine(dir, "thumb.png");
            processor.SaveResized(MakeImage(1000, 250), new Size(120, 30), path);

            Assert.AreEqual(new Size(120, 30), processor.Measure(File.ReadAllBytes(path)));
        }

        [TestMethod]
        public void The_file_extension_chooses_the_format()
        {
            // The System.Drawing code relied on Image.Save(path) inferring the encoder from
            // the extension - news images are saved under their uploaded extension. The
            // replacement has to behave the same or uploads silently change format.
            var jpeg = Path.Combine(dir, "photo.jpg");
            processor.Save(MakeImage(64, 64), jpeg);

            using (var stream = File.OpenRead(jpeg))
            {
                var format = Six.Image.DetectFormat(stream);
                Assert.AreEqual("JPEG", format.Name, "a .jpg path should produce a JPEG");
            }
        }

        [TestMethod]
        public void A_thumbnail_of_a_wide_image_keeps_its_proportions()
        {
            // The composition every migrated call site performs, end to end through a real
            // encoder this time rather than a recording double.
            var source = MakeImage(1600, 400);
            var size = processor.Measure(source);
            var target = new Size(256, ImageSizing.ProportionalHeight(size.Width, size.Height, 256));

            var path = Path.Combine(dir, "wide.png");
            processor.SaveResized(source, target, path);

            Assert.AreEqual(new Size(256, 64), processor.Measure(File.ReadAllBytes(path)));
        }

        [TestMethod]
        public void Resizing_to_a_single_pixel_does_not_throw()
        {
            // ImageSizing.BoundedByLongestSide clamps to 1 rather than 0 for extreme
            // aspect ratios; this is the other half of that guard.
            var path = Path.Combine(dir, "tiny.png");
            processor.SaveResized(MakeImage(4000, 3), new Size(256, 1), path);

            Assert.AreEqual(new Size(256, 1), processor.Measure(File.ReadAllBytes(path)));
        }

        /// <summary>A solid image of one colour, so a composite can be read back by colour.</summary>
        private static byte[] MakeSolid(int width, int height, Six.Color colour)
        {
            using (var image = new Six.Image<Rgba32>(width, height))
            using (var buffer = new MemoryStream())
            {
                image.Mutate(x => x.BackgroundColor(colour));
                image.Save(buffer, new Six.Formats.Png.PngEncoder());
                return buffer.ToArray();
            }
        }

        [TestMethod]
        public void SaveResizedJpeg_writes_a_jpeg_at_the_requested_size()
        {
            var path = Path.Combine(dir, "minimap.thumbnail.jpg");
            processor.SaveResizedJpeg(MakeImage(1024, 512), new Size(256, 128), path, 100);

            Assert.AreEqual(new Size(256, 128), processor.Measure(File.ReadAllBytes(path)));
            using (var stream = File.OpenRead(path))
                Assert.AreEqual("JPEG", Six.Image.DetectFormat(stream).Name);
        }

        [TestMethod]
        public void SaveResizedJpeg_actually_applies_the_quality()
        {
            // The reason quality is on the interface at all. PlasmaServer writes map thumbnails
            // at 100 and the galaxy render at 85; a method that quietly used the library default
            // would pass every other test here while making both of them worse.
            var source = MakeImage(400, 300);
            var high = Path.Combine(dir, "high.jpg");
            var low = Path.Combine(dir, "low.jpg");

            processor.SaveResizedJpeg(source, new Size(200, 150), high, 100);
            processor.SaveResizedJpeg(source, new Size(200, 150), low, 10);

            Assert.IsTrue(new FileInfo(high).Length > new FileInfo(low).Length,
                "quality 100 should produce a bigger file than quality 10");
        }

        [TestMethod]
        public void ComposeJpeg_keeps_the_background_size()
        {
            var composed = processor.ComposeJpeg(MakeImage(800, 600), new ImageOverlay[0], 85);

            Assert.AreEqual(new Size(800, 600), processor.Measure(composed));
            using (var stream = new MemoryStream(composed))
                Assert.AreEqual("JPEG", Six.Image.DetectFormat(stream).Name);
        }

        [TestMethod]
        public void ComposeJpeg_draws_each_overlay_where_it_was_told()
        {
            // The galaxy map. Planet icons are drawn at computed rectangles, and the failure to
            // guard against is an overlay that lands in the wrong place or not at all - which no
            // dimension check would notice, since the canvas is the background's size either way.
            var background = MakeSolid(200, 200, Six.Color.Blue);
            var planet = MakeSolid(10, 10, Six.Color.Red);

            var composed = processor.ComposeJpeg(background,
                new[] { new ImageOverlay(planet, new System.Drawing.Rectangle(100, 100, 40, 40)) }, 100);

            using (var image = Six.Image.Load<Rgba32>(composed))
            {
                var inside = image[120, 120];
                var outside = image[20, 20];

                // Compared loosely on purpose: JPEG is lossy, so this asserts which colour won,
                // not an exact value.
                Assert.IsTrue(inside.R > inside.B, "the overlay should be red at its centre");
                Assert.IsTrue(outside.B > outside.R, "the background should still be blue outside it");
            }
        }

        [TestMethod]
        public void ComposeJpeg_stretches_an_overlay_into_its_rectangle()
        {
            // Graphics.DrawImage(image, x, y, w, h) stretched; a library that letterboxed instead
            // would leave the corners of the rectangle showing the background.
            var background = MakeSolid(100, 100, Six.Color.Blue);
            var wide = MakeSolid(40, 10, Six.Color.Red);

            var composed = processor.ComposeJpeg(background,
                new[] { new ImageOverlay(wide, new System.Drawing.Rectangle(10, 10, 80, 80)) }, 100);

            using (var image = Six.Image.Load<Rgba32>(composed))
            {
                var nearBottomOfRectangle = image[50, 85];
                Assert.IsTrue(nearBottomOfRectangle.R > nearBottomOfRectangle.B,
                    "a 4:1 overlay told to fill a square should have been stretched to fill it");
            }
        }

        [TestCleanup]
        public void TearDown()
        {
            if (dir != null && Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
