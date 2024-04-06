using System;
using System.Drawing;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace ZkData.UnitSyncLib
{
    [Serializable]
    public class Map: ResourceInfo, ICloneable
    {
        public override ResourceType ResourceType { get; } = ResourceType.Map;

        [NonSerialized]
        [JsonIgnore]
        Image heightMap;

        [NonSerialized]
        [JsonIgnore]
        Image metalmap;

        [NonSerialized]
        [JsonIgnore]
        Bitmap minimap;

        public int ExtractorRadius { get; set; }
        public int Gravity { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public Image Heightmap { get { return heightMap; } set { heightMap = value; } }

        public float MaxMetal { get; set; }
        public int MaxWind { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public Image Metalmap { get { return metalmap; } set { metalmap = value; } }

        [XmlIgnore]
        [JsonIgnore]
        public Bitmap Minimap { get { return minimap; } set { minimap = value; } }

        public int MinWind { get; set; }

        public Option[] Options { get; set; }
        public StartPos[] Positions { get; set; }
        public Size Size { get; set; }
        public int TidalStrength { get; set; }


        public Map(ResourceInfo res) {
            res.FillTo(this);
        }

        public Map() {}
        
        public static string GetHumanName(string mapName)
        {
            if (mapName == null) throw new ArgumentNullException("mapName");
            return mapName.Replace('_', ' ').Replace(' ', ' ');
        }

        public override string ToString() => GetHumanName(Name);

        public object Clone() => MemberwiseClone();
    }
}