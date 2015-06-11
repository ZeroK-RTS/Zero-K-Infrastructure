using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace ZeroKLobby
{
    static class WindowsApi
    {
        public const UInt32 FLASHW_ALL = 3;
        
        public static TimeSpan IdleTime {
            get {
                var ret = CalculateIdleTime();
                return ret;
            }
        }

    
		private static bool haveXprintIdle = true;
        public static TimeSpan CalculateIdleTime() {
            if (Environment.OSVersion.Platform == PlatformID.Unix) {
				if (haveXprintIdle)
				{
					var ret = ZkData.Utils.ExecuteConsoleCommand("xprintidle");
                	if (ret != null) {
                    	int ms;
                    	if (int.TryParse(ret, out ms)) return TimeSpan.FromMilliseconds(ms);
					}else{ haveXprintIdle = false;} //some Linux might not have "xprintidle", stop trying, avoid spam in Diagnostic Log.
				}
				return DateTime.Now - Program.ToolTip.LastUserAction;
            }

            var systemUptime = Environment.TickCount; // Get the system uptime
            var idleTicks = 0; // The number of ticks that passed since last input

            var lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            // If we have a value from the function
            if (GetLastInputInfo(ref lastInputInfo)) {
                var lastInputTicks = (int)lastInputInfo.dwTime; // Get the number of ticks at the point when the last activity was seen
                idleTicks = systemUptime - lastInputTicks; // Number of idle ticks = system uptime ticks - number of ticks at last input
            }

            return TimeSpan.FromMilliseconds(idleTicks);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlashWindowEx(ref FLASHWINFO pwfi);


        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

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