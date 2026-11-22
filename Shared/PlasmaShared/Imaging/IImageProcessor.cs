using System.Collections.Generic;
using System.Drawing;

namespace PlasmaShared.Imaging
{
    /// <summary>
    /// The image operations the server performs on uploads, expressed without naming an
    /// imaging library.
    ///
    /// This is step 1 of Shared/PlasmaShared/IMAGING-MIGRATION.md. System.Drawing.Common
    /// is Windows-only on .NET 9, so the port needs these operations behind something that
    /// can be reimplemented - on ImageSharp, most likely. Nothing in this file names
    /// Bitmap, Image or Graphics; <see cref="Size"/> is from System.Drawing.Primitives,
    /// which is cross-platform, so this interface already compiles on .NET 9.
    ///
    /// Encoded bytes rather than a stream or an image object, because an upload is read
    /// once and then used twice - saved whole and saved again resized - and because bytes
    /// are the one currency every imaging library speaks.
    ///
    /// Resizing and saving are a single operation on purpose. Splitting them would mean
    /// encoding the resized image to bytes and then re-encoding it to the file, which for
    /// a JPEG target costs a generation of quality that the current code does not pay.
    /// </summary>
    public interface IImageProcessor
    {
        /// <summary>Dimensions of an encoded image, without decoding it fully if avoidable.</summary>
        Size Measure(byte[] image);

        /// <summary>Writes the image to a path, in the format that path's extension implies.</summary>
        void Save(byte[] image, string path);

        /// <summary>Resizes and writes in one step, in the format the path's extension implies.</summary>
        void SaveResized(byte[] image, Size target, string path);

        /// <summary>
        /// Resizes and writes as JPEG at an explicit quality.
        ///
        /// Separate from <see cref="SaveResized"/> because quality is not a detail here: the map
        /// thumbnail in PlasmaServer is written at 100 and the galaxy render at 85, both chosen
        /// deliberately, and an imaging library's default is neither. Porting those call sites
        /// through a quality-less method would change what the server stores while every test
        /// still passed.
        /// </summary>
        void SaveResizedJpeg(byte[] image, Size target, string path, int quality);

        /// <summary>
        /// Draws <paramref name="overlays"/> onto <paramref name="background"/>, in order, and
        /// returns the result encoded as JPEG at <paramref name="quality"/>.
        ///
        /// This is the galaxy map: a background with a planet icon drawn at each planet's
        /// position. It returns bytes rather than writing a file because the caller needs the
        /// dimensions too, and because bytes are what a test can look at.
        ///
        /// Each overlay is drawn stretched into its rectangle, which is what
        /// <c>Graphics.DrawImage(image, x, y, w, h)</c> did.
        /// </summary>
        byte[] ComposeJpeg(byte[] background, IReadOnlyList<ImageOverlay> overlays, int quality);
    }

    /// <summary>One image to draw onto another, and where to put it.</summary>
    public struct ImageOverlay
    {
        public ImageOverlay(byte[] image, Rectangle target)
        {
            Image = image;
            Target = target;
        }

        /// <summary>The encoded image to draw.</summary>
        public byte[] Image { get; }

        /// <summary>Where it goes on the background, in pixels. The image is stretched to fit.</summary>
        public Rectangle Target { get; }
    }
}
