using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PlanetWarsShared.UnitSyncLib
{
    public class UnitSync : IDisposable
    {
        const int AuthorBufferSize = 200;
        const int DefaultMapInfoVersion = 1;
        const int DescriptionBufferSize = 256;
        const int MaxMipLevel = 10;
        readonly string originalDirectory;
        bool disposed;
        Dictionary<uint, string> maps;

        public UnitSync(string path)
        {
            originalDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(path);
            if (!NativeMethods.Init(false, 0)) {
                throw new UnitSyncException("Unitsync initialization failed.");
            }
            Version = NativeMethods.GetSpringVersion();
            /*if (!Regex.IsMatch(Version, @"\d\.\d\db\d")) {
                throw new UnitSyncException(String.Format("Invalid Spring version ({0})", Version));
            }*/
        }

        public UnitSync() : this(DefaultSpringPath) {}
        public static string DefaultSpringPath { get; set; }

        public string Version { get; set; }

        #region IDisposable Members

        public void Dispose()
        {
            if (!disposed) {
                Directory.SetCurrentDirectory(originalDirectory);
                try {
                    NativeMethods.UnInit();
                } catch (DllNotFoundException) {
                    // do nothing, already thrown on init
                }
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        ~UnitSync()
        {
            Dispose();
        }

        public Dictionary<uint, string> GetMapNames()
        {
            if (disposed) {
                throw new ObjectDisposedException("Unitsync has already been released.");
            }
            maps = maps ?? (from i in Enumerable.Range(0, NativeMethods.GetMapCount())
                            let n = NativeMethods.GetMapName(i)
                            let c = NativeMethods.GetMapChecksum(i)
                            group n by c).ToDictionary(g => g.Key, g => g.First());
            return maps;
        }

        public MapInfo GetMapInfo(string mapName, int mapInfoVersion)
        {
            return GetMapInfo(GetMapChecksum(mapName), mapInfoVersion);
        }

        public MapInfo GetMapInfo(uint checksum)
        {
            return GetMapInfo(checksum, DefaultMapInfoVersion);
        }

        public MapInfo GetMapInfo(uint checksum, int mapInfoVersion)
        {
            if (disposed) {
                throw new ObjectDisposedException("Unitsync has already been released.");
            }
            string mapName = maps[checksum];
            if (!mapName.ToLower().EndsWith(".smf") && !(mapName.ToLower().EndsWith(".sm3"))) {
                throw new ArgumentException("Map name is invalid, must end with \".smf\" or \".sm3\".");
            }
            if (!new[] {0, 1}.Contains(mapInfoVersion)) {
                throw new ArgumentOutOfRangeException("mapInfoVersion", "must be 0 or 1.");
            }
            if (!GetMapNames().ContainsValue(mapName)) {
                throw new UnitSyncException(String.Format("Map not found ({0}).", mapName));
            }
            var mapInfo = new MapInfo
            {
                // make buffers
                Author = new String(' ', AuthorBufferSize),
                Description = new String(' ', DescriptionBufferSize)
            };
            if (!NativeMethods.GetMapInfoEx(mapName, ref mapInfo, mapInfoVersion)) {
                throw new UnitSyncException("Error getting map information.");
            }
            TestMapInfo(mapInfo);
            return mapInfo;
        }

        public uint GetMapChecksum(string mapName)
        {
            return GetMapNames().Single(kvp => kvp.Value == mapName).Key;
        }

        static void TestMapInfo(MapInfo mapInfo)
        {
            if (mapInfo.Description == null) {
                throw new UnitSyncException("Null description.");
            }
            if (mapInfo.Description.StartsWith("Parse error")) {
                throw new UnitSyncException("Parse error. This usually means the map is broken.");
            }
            if (mapInfo.Description.EndsWith("not found")) {
                throw new UnitSyncException("Map file not found. This usually means the map is broken.");
            }
            if (mapInfo.Width <= 0 || mapInfo.Height <= 0) {
                throw new UnitSyncException(
                    String.Format("Invalid map size. ({0}, {1})", mapInfo.Width, mapInfo.Height));
            }
            StartPos[] positions = mapInfo.Positions;
            if (positions == null) {
                throw new UnitSyncException("Error in loading start positions.");
            }
            if (mapInfo.ExtractorRadius < 0) {
                throw new UnitSyncException(String.Format("Invalid extractor radius ({0}).", mapInfo.ExtractorRadius));
            }
            if (mapInfo.Gravity < 0) {
                throw new UnitSyncException(String.Format("Invalid gravity ({0}).", mapInfo.Gravity));
            }
            if (mapInfo.MaxMetal < 0) {
                throw new UnitSyncException(string.Format("Invalid maximum metal ({0}).", mapInfo.MaxMetal));
            }
            if (mapInfo.MaxWind < 0) {
                throw new UnitSyncException(string.Format("Invalid maximum wind ({0}).", mapInfo.MaxWind));
            }
            if (mapInfo.MinWind < 0) {
                throw new UnitSyncException(string.Format("Invalid minimum wind ({0}).", mapInfo.MinWind));
            }
            if (mapInfo.MinWind > mapInfo.MaxWind) {
                throw new UnitSyncException(
                    string.Format(
                        "Minimum wind is higher than maximum wind ({0}, {1})", mapInfo.MinWind, mapInfo.MaxWind));
            }
            if (mapInfo.TidalStrength < 0) {
                throw new UnitSyncException(string.Format("Invalid tidal strength ({0}).", mapInfo.TidalStrength));
            }
        }

        public Bitmap GetMinimap(string mapName, int mipLevel)
        {
            if (disposed) {
                throw new ObjectDisposedException("Unitsync has already been disposed.");
            }
            if (!mapName.ToLower().EndsWith(".smf") && !(mapName.ToLower().EndsWith(".sm3"))) {
                throw new ArgumentException("Map name is invalid, must end with \".smf\" or \".sm3\".");
            }
            if (!GetMapNames().ContainsValue(mapName)) {
                throw new UnitSyncException(string.Format("Map not found ({0}).", mapName));
            }
            if (mipLevel < 0 || mipLevel > MaxMipLevel) {
                throw new ArgumentOutOfRangeException(
                    "mipLevel", string.Format("Mip level must range from 0 to {0}.", MaxMipLevel));
            }

            int size = 1024 >> mipLevel;
            IntPtr pointer = NativeMethods.GetMinimap(mapName, mipLevel);
            const PixelFormat format = PixelFormat.Format16bppRgb565;
            int pixelFormatSize = Image.GetPixelFormatSize(format)/8;
            int stride = size*pixelFormatSize;

            return new Bitmap(size, size, stride, format, pointer);
        }

        public MapInfo GetMapInfo(string mapName)
        {
            return GetMapInfo(mapName, DefaultMapInfoVersion);
        }

        public Bitmap GetHeightMap(string mapName)
        {
            return GetInfoMap(mapName, "height", 1);
        }

        public Bitmap GetMetalMap(string mapName)
        {
            return GetInfoMap(mapName, "metal", 1);
        }

        unsafe Bitmap GetInfoMap(string mapName, string name, int bytesPerPixel)
        {
            if (disposed) {
                throw new ObjectDisposedException("Unitsync has already been disposed.");
            }
            int width = 0;
            int height = 0;
            if (!NativeMethods.GetInfoMapSize(mapName, name, ref width, ref height)) {
                throw new UnitSyncException("GetInfoMapSize failed");
            }
            var infoMapData = new byte[width*height];
            var infoMapHandle = GCHandle.Alloc(infoMapData, GCHandleType.Pinned);
            try {
                var infoMapPointer = Marshal.UnsafeAddrOfPinnedArrayElement(infoMapData, 0);

                var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

                if (!NativeMethods.GetInfoMap(mapName, name, infoMapPointer, bytesPerPixel)) {
                    throw new UnitSyncException("GetInfoMap failed");
                }
                BitmapData bitmapData = bitmap.LockBits(
                    new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                const int PixelSize = 3;
                var p = (byte*)bitmapData.Scan0;
                for (int i = 0; i < infoMapData.Length; i++) {
                    var v = infoMapData[i];
                    var d = i/width*bitmapData.Stride + i%width*PixelSize;
                    p[d] = p[d + 1] = p[d + 2] = v;
                }
                bitmap.UnlockBits(bitmapData);
                return bitmap;
            } finally {
                infoMapHandle.Free();
            }
        }
    }
}