using System.IO;
using System.Threading;

namespace PlasmaDownloader.Packages
{
    public static class StreamExtension
    {
        public static bool ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        {
            long val = 0;
            return ReadExactly(stream, buffer, offset, count, ref val);
        }

        public static bool ReadExactly(this Stream stream, byte[] buffer, int offset, int count, ref long valueToIncrement)
        {
            var read = 0;
            do
            {
                var increment = stream.Read(buffer, offset + read, count - offset - read);
                if (increment == 0) return false;
                read += increment;
                Interlocked.Add(ref valueToIncrement, increment);
            } while (read < count);
            return true;
        }
    }
}