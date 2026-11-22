using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace PlasmaShared.Imaging
{
    /// <summary>
    /// <see cref="IImageProcessor"/> on System.Drawing - what the server has always used.
    ///
    /// Deliberately a thin wrapper: every method does exactly what the call site it
    /// replaced did, in the same order, so introducing the seam changes no pixels. The
    /// interesting version of this class is the one that does not exist yet, on a
    /// cross-platform library; this one exists so that swapping to it touches one file
    /// instead of six call sites.
    /// </summary>
    public class SystemDrawingImageProcessor : IImageProcessor
    {
        public Size Measure(byte[] image)
        {
            using (var stream = new MemoryStream(image))
            using (var loaded = Image.FromStream(stream))
                return loaded.Size;
        }

        public void Save(byte[] image, string path)
        {
            using (var stream = new MemoryStream(image))
            using (var loaded = Image.FromStream(stream))
                loaded.Save(path);
        }

        public void SaveResized(byte[] image, Size target, string path)
        {
            using (var stream = new MemoryStream(image))
            using (var loaded = Image.FromStream(stream))
            using (var resized = loaded.GetResized(target.Width, target.Height, InterpolationMode.HighQualityBicubic))
                resized.Save(path);
        }
    
        public void SaveResizedJpeg(byte[] image, Size target, string path, int quality)
        {
            using (var stream = new MemoryStream(image))
            using (var loaded = Image.FromStream(stream))
            using (var resized = new Bitmap(target.Width, target.Height, PixelFormat.Format24bppRgb))
            {
                using (var graphics = Graphics.FromImage(resized))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(loaded, 0, 0, target.Width, target.Height);
                }

                var encoder = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
                var parameters = new EncoderParameters(1);
                parameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
                resized.Save(path, encoder, parameters);
            }
        }

        public byte[] ComposeJpeg(byte[] background, IReadOnlyList<ImageOverlay> overlays, int quality)
        {
            using (var backgroundStream = new MemoryStream(background))
            using (var loaded = Image.FromStream(backgroundStream))
            using (var canvas = new Bitmap(loaded.Width, loaded.Height))
            {
                using (var graphics = Graphics.FromImage(canvas))
                {
                    graphics.DrawImage(loaded, 0, 0, canvas.Width, canvas.Height);

                    foreach (var overlay in overlays)
                    {
                        using (var overlayStream = new MemoryStream(overlay.Image))
                        using (var overlayImage = Image.FromStream(overlayStream))
                            graphics.DrawImage(overlayImage, overlay.Target.X, overlay.Target.Y,
                                               overlay.Target.Width, overlay.Target.Height);
                    }
                }

                var encoder = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
                var parameters = new EncoderParameters(1);
                parameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);

                using (var output = new MemoryStream())
                {
                    canvas.Save(output, encoder, parameters);
                    return output.ToArray();
                }
            }
        }
}
}
