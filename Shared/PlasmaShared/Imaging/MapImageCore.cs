// SixLabors.ImageSharp is imported wholesale for the same reason ImageSharpImageProcessor does it:
// Save and Mutate are extension methods and an alias does not bring extensions into scope. That
// collides with System.Drawing.Size, so that one is spelled out where it appears.
using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PlasmaShared.Imaging
{
    /// <summary>
    ///     A map image on its way out of unitsync - minimap, heightmap or metalmap.
    ///
    ///     The .NET 9 half of a compat twin; MapImage.cs is the .NET Framework one. Same name, same
    ///     members, so <c>UnitSync</c> and <c>Map</c> - which compile in both stacks from one
    ///     source - name this type and neither knows which implementation it got.
    ///
    ///     **It holds the pixels, not an image object.** unitsync hands back an RGB24 buffer and
    ///     <see cref="PixelBuffers" /> has already unpacked it in managed code, so there is nothing
    ///     to decode: keeping the array is both the cheapest thing and the one that needs no
    ///     library at all until something asks for a resize or an encode. Those two go through
    ///     ImageSharp, which is what <see cref="Images" /> already chose for the rest of the site.
    ///
    ///     Deliberately no Underlying property. The Framework twin exposes its System.Drawing.Image;
    ///     anything reaching for that does not compile here, which is how the port finds such
    ///     callers rather than discovering them at runtime.
    /// </summary>
    public sealed class MapImage : IDisposable
    {
        private readonly byte[] rgb24;
        private readonly System.Drawing.Size size;

        private MapImage(byte[] rgb24, System.Drawing.Size size)
        {
            this.rgb24 = rgb24;
            this.size = size;
        }

        public static MapImage FromRgb24(byte[] pixels, System.Drawing.Size size)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (pixels.Length < size.Width * size.Height * 3)
                throw new ArgumentException("buffer is smaller than " + size.Width + "x" + size.Height, nameof(pixels));
            return new MapImage(pixels, size);
        }

        public System.Drawing.Size Size => size;

        /// <summary>
        ///     Bicubic, to ask for what the GDI+ twin asks for. The two will not be pixel-identical
        ///     - different libraries, different kernels - which is the same caveat recorded for
        ///     Images.Processor when that switched.
        /// </summary>
        public MapImage ResizeTo(System.Drawing.Size newSize)
        {
            using (var image = Load())
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(newSize.Width, newSize.Height),
                    Sampler = KnownResamplers.Bicubic,
                    Mode = ResizeMode.Stretch,
                }));

                var pixels = new byte[newSize.Width * newSize.Height * 3];
                image.CopyPixelDataTo(pixels);
                return new MapImage(pixels, newSize);
            }
        }

        public byte[] ToBytes(int maxSize)
        {
            var bounded = ImageSizing.BoundedByLongestSide(size.Width, size.Height, maxSize);
            using (var image = Load())
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(bounded.Width, bounded.Height),
                    Sampler = KnownResamplers.Bicubic,
                    Mode = ResizeMode.Stretch,
                }));

                using (var stream = new MemoryStream())
                {
                    image.Save(stream, new JpegEncoder());
                    return stream.ToArray();
                }
            }
        }

        private Image<Rgb24> Load()
        {
            return Image.LoadPixelData<Rgb24>(rgb24, size.Width, size.Height);
        }

        public void Dispose()
        {
            // Nothing native is held: the pixels are a managed array. Present so that callers
            // written against the Framework twin, which does own a GDI+ handle, compile unchanged.
        }
    }
}
