using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ChobbyLauncher
{
    static class WindowsApi
    {
        public const UInt32 FLASHW_ALL = 3;
    
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        
        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

    }
}