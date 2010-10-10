using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using PlanetWars.Utility;
using PlanetWarsShared.UnitSyncLib;

namespace PlanetWars
{
    [Serializable]
    public class Map
    {
        static readonly BinaryFormatter serializer = new BinaryFormatter();

        [NonSerialized]
        Image heightMap;

        [NonSerialized]
        Image metalMap;

        [NonSerialized]
        Image minimap;

        public Map(string name, MapInfo mapInfo, uint? checksum) : this(name, mapInfo)
        {
            Checksum = checksum;
        }

        public Map(string name, MapInfo mapInfo)
        {
            Name = name;
            Description = mapInfo.Description;
            TidalStrength = mapInfo.TidalStrength;
            Gravity = mapInfo.Gravity;
            MaxMetal = mapInfo.MaxMetal;
            ExtractorRadius = mapInfo.ExtractorRadius;
            MinWind = mapInfo.MinWind;
            MaxWind = mapInfo.MaxWind;
            Author = mapInfo.Author;
            Size = new Size(mapInfo.Width, mapInfo.Height);
            StartPos[] positions = mapInfo.Positions;
            Positions = new Point[mapInfo.PosCount];
            for (int i = 0; i < mapInfo.PosCount; i++) {
                Positions[i] = new Point(positions[i].X, positions[i].Z);
            }
            HumanName = GetHumanName(name);
        }

        public uint? Checksum { get; set; }

        public Image Minimap
        {
            get { return minimap; }
            set { minimap = value; }
        }

        public string HumanName { get; private set; }
        public string Name { get; private set; }
        public Size Size { get; private set; }
        public string Description { get; private set; }
        public int TidalStrength { get; private set; }
        public int Gravity { get; private set; }
        public float MaxMetal { get; private set; }
        public int ExtractorRadius { get; private set; }
        public int MinWind { get; private set; }
        public int MaxWind { get; private set; }
        public string Author { get; private set; }
        public Point[] Positions { get; private set; }

        public Image MetalMap
        {
            get { return metalMap; }
            set { metalMap = value; }
        }

        public Image HeightMap
        {
            get { return heightMap; }
            set { heightMap = value; }
        }

        public static string GetHumanName(string mapName)
        {
            if (mapName == null) {
                throw new ArgumentNullException("mapName");
            }
            return mapName.Replace('_', ' ').Replace(' ', ' ').Substring(0, mapName.Length - 4);
        }

        public Bitmap FixAspectRatio(Image squareMinimap)
        {
            float mapRatio = (float)Size.Width/Size.Height;
            Size newSize = mapRatio > 1
                               ? new Size(squareMinimap.Size.Width, (int)(squareMinimap.Size.Height/mapRatio))
                               : new Size((int)(squareMinimap.Size.Width*mapRatio), squareMinimap.Size.Height);

            // correctMinimap = new Bitmap(squareMinimap, newSize); // quick, but how to specify interpolation?
            Bitmap correctMinimap = new Bitmap(newSize.Width, newSize.Height, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(correctMinimap)) {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(
                    squareMinimap,
                    new Rectangle(Point.Empty, newSize),
                    new Rectangle(Point.Empty, squareMinimap.Size),
                    GraphicsUnit.Pixel);
            }
            return correctMinimap;
        }

        public override string ToString()
        {
            return HumanName;
        }

        public static Map Load(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open)) {
                return (Map)serializer.Deserialize(stream);
            }
        }

        public static Map Load(Stream stream)
        {
            return (Map)serializer.Deserialize(stream);
        }

        public void Save(string path)
        {
            using (var stream = new FileStream(path, FileMode.Create)) {
                serializer.Serialize(stream, this);
            }
        }

        public static Map FromDisk(string mapName)
        {

            var mapInfo = Program.MapInfoCache.Combine(mapName + ".dat");
            var minimap = Program.MinimapCache.Combine(mapName + ".jpg");
            var heightmap = Program.HeightmapCache.Combine(mapName + ".jpg");
            var metalmap = Program.MetalmapCache.Combine(mapName + ".png");
            if (File.Exists(mapInfo) && File.Exists(minimap) && File.Exists(heightmap) && File.Exists(metalmap)) {
            	var map = Load(mapInfo);
            	map.Minimap = map.HeightMap = map.MetalMap = new Bitmap(1, 1);
#if false
                map.Minimap = Image.FromFile(minimap);
                map.HeightMap = Image.FromFile(heightmap);
                map.MetalMap = Image.FromFile(metalmap);
#endif
                return map;
            }
            return null;
        }
    }
}