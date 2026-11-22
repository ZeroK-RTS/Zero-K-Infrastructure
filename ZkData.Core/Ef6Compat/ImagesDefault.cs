namespace PlasmaShared.Imaging
{
    /// <summary>
    /// The image processor for the .NET 9 build.
    ///
    /// PlasmaShared's own <c>Images</c> defaults to the System.Drawing implementation,
    /// which cannot exist here - so this build gets the ImageSharp one. That is the
    /// destination anyway; on .NET 9 there is no other option.
    /// </summary>
    public static class Images
    {
        public static IImageProcessor Processor = new ImageSharpImageProcessor();

        public static byte[] ReadAll(System.IO.Stream stream)
        {
            using (var buffer = new System.IO.MemoryStream())
            {
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }
    }
}
