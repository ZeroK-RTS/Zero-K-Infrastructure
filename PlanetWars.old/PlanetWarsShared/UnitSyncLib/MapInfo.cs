using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Xml.Serialization;

namespace PlanetWarsShared.UnitSyncLib
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MapInfo // If the fields in this class are changed, it might not work with unitsync anymore.
    {
        [MarshalAs(UnmanagedType.LPStr)]
        string description;

        int tidalStrength;

        public int TidalStrength
        {
            get { return tidalStrength; }
            set { tidalStrength = value; }
        }
        int gravity;

        public int Gravity
        {
            get { return gravity; }
            set { gravity = value; }
        }
        float maxMetal;

        public float MaxMetal
        {
            get { return maxMetal; }
            set { maxMetal = value; }
        }
        int extractorRadius;

        public int ExtractorRadius
        {
            get { return extractorRadius; }
            set { extractorRadius = value; }
        }
        int minWind;

        public int MinWind
        {
            get { return minWind; }
            set { minWind = value; }
        }
        int maxWind;

        public int MaxWind
        {
            get { return maxWind; }
            set { maxWind = value; }
        }
        int width;

        public int Width
        {
            get { return width; }
            set { width = value; }
        }
        int height;

        public int Height
        {
            get { return height; }
            set { height = value; }
        }
        int posCount;

        public int PosCount
        {
            get { return posCount; }
            set { posCount = value; }
        }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.Struct)]
        StartPos[] positions;

        public StartPos[] Positions
        {
            get { return positions; }
            set { positions = value; }
        }

        [MarshalAs(UnmanagedType.LPStr)]
        string author;

        public string Author
        {
            get { return author; }
            set { author = value; }
        }

        public string Description
        {
            get { return description;}
            set { description = value; }
        }

 

        public void Save(string path)
        {
            using (var stream = new FileStream(path, FileMode.Create))
                new XmlSerializer(typeof(MapInfo)).Serialize(stream, this);
        }

        public static MapInfo FromFile(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                return (MapInfo)new XmlSerializer(typeof(MapInfo)).Deserialize(stream);
            }
        }

        public static MapInfo FromString(string xmlString)
        {
            return (MapInfo)new XmlSerializer(typeof(MapInfo)).Deserialize(new StringReader(xmlString));
        }
    }
}