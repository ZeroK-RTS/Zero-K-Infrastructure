using System;
using System.Drawing;
using System.Xml.Serialization;

namespace ZkData.UnitSyncLib
{
    [Serializable]
    public class Map: IResourceInfo, ICloneable
    {
        [NonSerialized]
        Image heightMap;

        [NonSerialized]
        Image metalmap;

        [NonSerialized]
        Bitmap minimap;

        public string Author { get; set; }

        public string Description { get; set; }
        public int ExtractorRadius { get; set; }
        public int Gravity { get; set; }

        [XmlIgnore]
        public Image Heightmap { get { return heightMap; } set { heightMap = value; } }

        public string HumanName { get; set; }
        public float MaxMetal { get; set; }
        public int MaxWind { get; set; }

        [XmlIgnore]
        public Image Metalmap { get { return metalmap; } set { metalmap = value; } }

        [XmlIgnore]
        public Bitmap Minimap { get { return minimap; } set { minimap = value; } }

        public int MinWind { get; set; }
        public string Name { get; set; }

        public Option[] Options { get; set; }
        public StartPos[] Positions { get; set; }
        public Size Size { get; set; }
        public int TidalStrength { get; set; }

        public Map(string name)
        {
            Name = name;
            HumanName = GetHumanName(name);
        }

        public Map() {}


        public static string GetHumanName(string mapName)
        {
            if (mapName == null) throw new ArgumentNullException("mapName");
            return mapName.Replace('_', ' ').Replace(' ', ' ');
        }

        public override string ToString()
        {
            return HumanName;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }


        string IResourceInfo.Name { get { return Name; } set { Name = value; } }

        public string ArchiveName { get; set; }
    }
}