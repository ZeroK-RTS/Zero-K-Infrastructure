using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace CMissionLib.UnitSyncLib
{
	[Serializable]
	public class Map : ICloneable
	{
		[NonSerialized] Image heightMap;

		[NonSerialized] Image metalmap;

		[NonSerialized] Bitmap minimap;

		public Map(string name)
		{
			Name = name;
			HumanName = GetHumanName(name);
		}

		public Map() {}

		public string Author { get; set; }

		public string Description { get; set; }
		public int ExtractorRadius { get; set; }
		public int Gravity { get; set; }

		[XmlIgnore]
		public Image Heightmap
		{
			get { return heightMap; }
			set { heightMap = value; }
		}

		public string HumanName { get; set; }
		public float MaxMetal { get; set; }
		public int MaxWind { get; set; }

		[XmlIgnore]
		public Image Metalmap
		{
			get { return metalmap; }
			set { metalmap = value; }
		}

		[XmlIgnore]
		public Bitmap Minimap
		{
			get { return minimap; }
			set { minimap = value; }
		}

		public int MinWind { get; set; }
		public string Name { get; set; }

		public IEnumerable<Option> Options { get; set; }
		public StartPos[] Positions { get; set; }
		public Size Size { get; set; }
		public int TidalStrength { get; set; }

		public BitmapSource Texture { get; set; }

		public int Checksum { get; set; }

		public string ArchiveName { get; set; }

		#region ICloneable Members

		public object Clone()
		{
			return MemberwiseClone();
		}

		#endregion

		public static string GetHumanName(string mapName)
		{
			if (mapName == null) throw new ArgumentNullException("mapName");
			return mapName.Replace('_', ' ').Replace(' ', ' ');
		}

		public override string ToString()
		{
			return HumanName;
		}
	}
}