using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace ZkData.UnitSyncLib
{
	[Serializable]
	public class Mod: ResourceInfo
	{
        public override ResourceType ResourceType { get; } = ResourceType.Mod;

        public bool IsMission { get { return !string.IsNullOrEmpty(MissionScript); } }
		public string MissionScript { get; set; }
		public List<MissionSlot> MissionSlots = new List<MissionSlot>();
		public Ai[] ModAis { get; set; }
		public string Mutator { get; set; }
		public Option[] Options { get; set; }
		public string MissionMap { get; set; }

		public byte[][] SideIcons { get; set; }
		public string[] Sides { get; set; }
		public SerializableDictionary<string, string> StartUnits { get; set; }
		public UnitInfo[] UnitDefs { get; set; }

	    public Mod(ResourceInfo res) {
	        res.FillTo(this);
	    }

	    public Mod() {}

	    public override string ToString()
		{
			return Name;
		}
	}
}