using System;

namespace ZkData
{
    [Serializable]
    public class PoolFileInfo
    {
        public uint Crc;
        public Hash Hash;
        public string Name;
        public uint UncompressedSize;
    } ;
}