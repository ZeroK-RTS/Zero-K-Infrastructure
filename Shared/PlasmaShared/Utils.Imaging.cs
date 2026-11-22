using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using PlasmaShared.Imaging;
using Encoder = System.Drawing.Imaging.Encoder;

namespace PlasmaShared
{
    /// <summary>
    /// The half of <see cref="Utils"/> that needs System.Drawing.Common, split out for the same
    /// reason as Utils.Enumerable.cs and Utils.Polyfills.cs.
    ///
    /// System.Drawing.Common is Windows-only on .NET 9, and these four methods were the largest
    /// single thing keeping ZkLobbyServer.Core tied to it - 24 of the compile errors that stand
    /// between that project and dropping the package. **Nothing on the lobby server's side calls
    /// them**: every caller is ZeroKLobby, ChobbyLauncher or AutoRegistrator, all .NET Framework
    /// and all Windows. So the .NET 9 build of the server excludes this file rather than porting
    /// it, and the Framework build is unchanged.
    ///
    /// This is not the imaging migration - see IMAGING-MIGRATION.md, which is about the seam that
    /// replaces these with ImageSharp for callers that DO need them. This only stops a project
    /// carrying code none of its own code can reach.
    /// </summary>
    public static partial class Utils
    {
        public static Bitmap GetResized(this Image original, int newWidth, int newHeight, InterpolationMode mode = InterpolationMode.HighQualityBicubic)
        {
            var resized = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = mode;
                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }
            return resized;
        }

        public static Image GetResizedWithCache(this Image original, int newWidth, int newHeight, InterpolationMode mode = InterpolationMode.HighQualityBicubic)
        {
            return ResizedImageCache.Instance.GetResizedWithCache(original, newWidth, newHeight, mode);
        }

        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats 
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec 
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        }

        public static void SaveJpeg(this Image img, string path, int quality)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");


            // Encoder parameter for image quality 
            EncoderParameter qualityParam =
                    new EncoderParameter(Encoder.Quality, quality);
            // Jpeg image codec 
            ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");

            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;

            img.Save(path, jpegCodec, encoderParams);
        }

        public static byte[] ToBytes(this Image image, int size)
        {
            var stream = new MemoryStream();

            // Until 2026-09-25 this applied the image's OWN aspect ratio to an image that was
            // already in proportion - so a 2:1 minimap was stored 4:1 - and ignored the size
            // argument entirely, storing full-resolution images where 256 was asked for. Both
            // are fixed here, for newly registered maps only; images already stored are left
            // alone and can be found with ImageSizing.LooksLikeLegacyToBytes. The decision and
            // what it rules out are in IMAGING-MIGRATION.md.
            var newSize = ImageSizing.BoundedByLongestSide(image.Size.Width, image.Size.Height, size);
            var resizedImage = new Bitmap(newSize.Width, newSize.Height, PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(resizedImage))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, new Rectangle(Point.Empty, newSize), new Rectangle(Point.Empty, image.Size), GraphicsUnit.Pixel);
            }
            resizedImage.Save(stream, ImageFormat.Jpeg);
            stream.Position = 0;
            return stream.ToArray();
        }
    }
}
