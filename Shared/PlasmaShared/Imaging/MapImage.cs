using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PlasmaShared.Imaging
{
    /// <summary>
    ///     A map image on its way out of unitsync - minimap, heightmap or metalmap.
    ///
    ///     The .NET Framework half of a compat twin; <see cref="MapImage" /> in MapImageCore.cs is
    ///     the .NET 9 one. Same name, same members, so <c>UnitSync</c> and <c>Map</c> - which
    ///     compile in both stacks from one source - name this type and neither knows which
    ///     implementation it got.
    ///
    ///     **Why the type exists at all.** unitsync hands back raw pixel buffers, and everything up
    ///     to and including unpacking them is already portable: the interop binds and marshals on
    ///     .NET 9, and <see cref="PixelBuffers" /> does the 5-6-5 and greyscale expansion in managed
    ///     code. The only thing that was not portable was the last step - turning a finished RGB24
    ///     array into an image object, which was <c>System.Drawing.Bitmap</c>. Measured against a
    ///     real engine and a real map; see Tests.Portable/UnitSyncEngineTests and
    ///     IMAGING-MIGRATION.md.
    ///
    ///     This half keeps GDI+, because the Framework stack is what registers maps today and its
    ///     output should not change. It is the only remaining caller of
    ///     <see cref="GdiPixelBridge" />.
    /// </summary>
    public sealed class MapImage : IDisposable
    {
        private readonly Image image;

        private MapImage(Image image)
        {
            this.image = image;
        }

        public static MapImage FromRgb24(byte[] pixels, Size size)
        {
            return new MapImage(GdiPixelBridge.FromRgb24(pixels, size));
        }

        public Size Size
        {
            get { return image.Size; }
        }

        /// <summary>
        ///     The GDI+ image itself, for Framework-only callers. Absent from the .NET 9 twin on
        ///     purpose: anything reaching for it does not compile there, which is the point.
        /// </summary>
        public Image Underlying
        {
            get { return image; }
        }

        /// <summary>
        ///     Bicubic, matching what <c>FixAspectRatio</c> asked for when it did this inline.
        /// </summary>
        public MapImage ResizeTo(Size newSize)
        {
            var resized = new Bitmap(newSize.Width, newSize.Height, PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, new Rectangle(Point.Empty, newSize));
            }
            return new MapImage(resized);
        }

        /// <summary>
        ///     JPEG bytes, bounded by the longest side - what AutoRegistrator uploads.
        /// </summary>
        public byte[] ToBytes(int maxSize)
        {
            return Utils.ToBytes(image, maxSize);
        }

        public void Dispose()
        {
            image.Dispose();
        }
    }
}
