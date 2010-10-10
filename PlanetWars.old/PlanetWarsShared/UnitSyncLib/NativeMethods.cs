using System;
using System.Runtime.InteropServices;

namespace PlanetWarsShared.UnitSyncLib
{
    class NativeMethods
    {
        const string UnitSyncName = "unitsync";

        [DllImport(UnitSyncName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool Init([MarshalAs(UnmanagedType.I1)] bool isServer, int id);

        [DllImport(UnitSyncName)]
        public static extern void UnInit();

        [DllImport(UnitSyncName)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string GetSpringVersion();

        [DllImport(UnitSyncName)]
        public static extern int GetMapCount();

        [DllImport(UnitSyncName)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string GetMapName(int index);

        [DllImport(UnitSyncName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool GetMapInfoEx([MarshalAs(UnmanagedType.LPStr)] string name,
                                               [In, Out] ref MapInfo outInfo,
                                               int version);

        [DllImport(UnitSyncName)]
        public static extern uint GetMapChecksum(int index);

        [DllImport(UnitSyncName)]
        public static extern IntPtr GetMinimap(string mapName, int mipLevel);

        [DllImport(UnitSyncName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool GetInfoMapSize([In, MarshalAs(UnmanagedType.LPStr)] string filename,
                                                 [In, MarshalAs(UnmanagedType.LPStr)] string name,
                                                 ref int width,
                                                 ref int height);

        [DllImport(UnitSyncName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool GetInfoMap([In, MarshalAs(UnmanagedType.LPStr)] string filename,
                                             [In, MarshalAs(UnmanagedType.LPStr)] string name,
                                             IntPtr data,
                                             int typeHint);
    }
}