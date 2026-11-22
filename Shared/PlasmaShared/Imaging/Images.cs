using System.IO;

namespace PlasmaShared.Imaging
{
    /// <summary>
    /// The image processor the server uses. One place to change when the implementation
    /// moves off System.Drawing, and one place for a test to substitute.
    ///
    /// **This is ImageSharp as of 2026-09-21, and that changes bytes on disk.** Images
    /// uploaded from now on - clan avatars and backgrounds, news and lobby-news thumbnails,
    /// structure icons - are encoded by a different library than the ones already stored.
    /// Both implementations ask for bicubic resampling; different libraries with different
    /// kernels do not produce identical pixels, so new and old images differ slightly. No
    /// stored image is touched.
    ///
    /// It was flipped deliberately rather than left until the port forced it.
    /// System.Drawing.Common is Windows-only on .NET 9, so this line had to move before the
    /// port completes; doing it now means the difference lands while there is slack to look
    /// at it, and one line reverts it.
    /// </summary>
    public static class Images
    {
        public static IImageProcessor Processor = new ImageSharpImageProcessor();

        /// <summary>Reads an upload into memory. Uploads are used twice - stored, and stored resized.</summary>
        public static byte[] ReadAll(Stream stream)
        {
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }
    }
}
