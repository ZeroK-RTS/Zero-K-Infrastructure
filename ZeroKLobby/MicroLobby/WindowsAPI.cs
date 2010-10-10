using System;
using System.Runtime.InteropServices;

namespace SpringDownloader.MicroLobby
{
    static class WindowsApi
    {
        public static TimeSpan IdleTime
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix) return TimeSpan.Zero;

                var systemUptime = Environment.TickCount; // Get the system uptime
                var idleTicks = 0; // The number of ticks that passed since last input

                var lastInputInfo = new LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
                lastInputInfo.dwTime = 0;

                // If we have a value from the function
                if (GetLastInputInfo(ref lastInputInfo))
                {
                    var lastInputTicks = (int)lastInputInfo.dwTime; // Get the number of ticks at the point when the last activity was seen
                    idleTicks = systemUptime - lastInputTicks; // Number of idle ticks = system uptime ticks - number of ticks at last input
                }

                return TimeSpan.FromMilliseconds(idleTicks);
            }
        }

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern bool DwmIsCompositionEnabled();

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }
    }
}