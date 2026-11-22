using System;
using System.Runtime.InteropServices;

namespace PlasmaShared.Imaging
{
    /// <summary>
    ///     Copies a native buffer out, so nothing downstream holds a pointer unitsync owns and may
    ///     free.
    ///
    ///     Split out of <see cref="GdiPixelBridge" /> when the unitsync path stopped needing GDI+
    ///     except at the very end: this half never did. It is Marshal.Copy and nothing else, so it
    ///     compiles in both stacks and the .NET 9 build no longer has to link a file full of
    ///     Bitmap to get at it.
    /// </summary>
    public static class NativeBuffer
    {
        public static byte[] CopyFrom(IntPtr source, int length)
        {
            var buffer = new byte[length];
            Marshal.Copy(source, buffer, 0, length);
            return buffer;
        }
    }
}
