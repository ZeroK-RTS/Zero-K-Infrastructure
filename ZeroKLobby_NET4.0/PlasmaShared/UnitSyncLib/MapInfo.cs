using System;
using System.Runtime.InteropServices;

namespace ZkData.UnitSyncLib
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct MapInfo
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string description;
        public int tidalStrength;
        public int gravity;
        public float maxMetal;
        public int extractorRadius;
        public int minWind;
        public int maxWind;
        public int width;
        public int height;
        public int posCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.Struct)]
        public StartPos[] positions;

        [MarshalAs(UnmanagedType.LPStr)]
        public string author;
    }
}