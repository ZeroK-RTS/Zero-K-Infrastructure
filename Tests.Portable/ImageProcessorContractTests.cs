using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared.Imaging;

namespace Tests.Portable
{
    /// <summary>
    /// That <see cref="IImageProcessor"/> is portable is the whole point of it: the port
    /// needs these operations expressed without naming System.Drawing.Common, which is
    /// Windows-only on .NET 9.
    ///
    /// This file is the evidence. It implements the interface and drives it on .NET 9, on
    /// Linux, with no imaging library present at all. If someone adds a Bitmap or an Image
    /// to that interface, this stops compiling - which is the point, and is a better guard
    /// than a comment asking them not to.
    /// </summary>
    [TestClass]
    public class ImageProcessorContractTests
    {
        /// <summary>Records calls instead of touching pixels. Also what a future test double looks like.</summary>
        private class RecordingProcessor : IImageProcessor
        {
            public readonly List<string> Calls = new List<string>();
            public Size Reported = new Size(800, 600);

            public Size Measure(byte[] image)
            {
                Calls.Add("Measure(" + image.Length + ")");
                return Reported;
            }

            public void Save(byte[] image, string path)
            {
                Calls.Add("Save(" + image.Length + " -> " + path + ")");
            }

            public void SaveResized(byte[] image, Size target, string path)
            {
                Calls.Add("SaveResized(" + image.Length + " -> " + target.Width + "x" + target.Height + " -> " + path + ")");
            }

            public void SaveResizedJpeg(byte[] image, Size target, string path, int quality)
            {
                Calls.Add("SaveResizedJpeg(" + image.Length + " -> " + target.Width + "x" + target.Height +
                          " -> " + path + " @" + quality + ")");
            }

            public byte[] ComposeJpeg(byte[] background, IReadOnlyList<ImageOverlay> overlays, int quality)
            {
                Calls.Add("ComposeJpeg(" + background.Length + " + " + overlays.Count + " overlays @" + quality + ")");
                return background;
            }
        }

        [TestMethod]
        public void The_interface_can_be_implemented_without_an_imaging_library()
        {
            IImageProcessor processor = new RecordingProcessor();
            var bytes = new byte[] { 1, 2, 3, 4 };

            var size = processor.Measure(bytes);
            processor.Save(bytes, "/tmp/original.png");
            processor.SaveResized(bytes, new Size(64, 64), "/tmp/small.png");
            processor.SaveResizedJpeg(bytes, new Size(32, 32), "/tmp/thumb.jpg", 100);
            processor.ComposeJpeg(bytes, new[] { new ImageOverlay(bytes, new Rectangle(1, 2, 3, 4)) }, 85);

            Assert.AreEqual(new Size(800, 600), size);
            CollectionAssert.AreEqual(new[]
            {
                "Measure(4)",
                "Save(4 -> /tmp/original.png)",
                "SaveResized(4 -> 64x64 -> /tmp/small.png)",
                "SaveResizedJpeg(4 -> 32x32 -> /tmp/thumb.jpg @100)",
                "ComposeJpeg(4 + 1 overlays @85)",
            }, ((RecordingProcessor)processor).Calls);
        }

        [TestMethod]
        public void A_thumbnail_target_composes_from_the_measured_size()
        {
            // The shape every migrated call site now has: measure, then resize to a width
            // with a proportional height. Worth pinning as a composition, since the two
            // halves live in different files.
            var processor = new RecordingProcessor { Reported = new Size(1000, 250) };
            var bytes = new byte[] { 9 };

            var size = processor.Measure(bytes);
            var target = new Size(120, ImageSizing.ProportionalHeight(size.Width, size.Height, 120));
            processor.SaveResized(bytes, target, "/tmp/thumb.jpg");

            Assert.AreEqual(new Size(120, 30), target);
            Assert.AreEqual("SaveResized(1 -> 120x30 -> /tmp/thumb.jpg)", processor.Calls[1]);
        }
    }
}
