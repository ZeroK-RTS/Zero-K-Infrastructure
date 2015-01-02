using System;
using ZkData.UnitSyncLib;

namespace ZkData
{
    public class MapRegisteredEventArgs: EventArgs
    {
        public byte[] HeightMap { get; set; }
        public Map Map { get; set; }
        public string MapName { get; set; }
        public byte[] MetalMap { get; set; }
        public byte[] Minimap { get; set; }
        public byte[] SerializedData { get; set; }

        public MapRegisteredEventArgs(string mapName, Map map, byte[] minimap, byte[] metalMap, byte[] heightMap, byte[] serializedData)
        {
            MapName = mapName;
            Map = map;
            Minimap = minimap;
            MetalMap = metalMap;
            HeightMap = heightMap;
            SerializedData = serializedData;
        }
    }
}