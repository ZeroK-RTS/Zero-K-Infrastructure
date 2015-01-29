using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace ZkData.UnitSyncLib
{
	[Serializable]
	public class Mod: IResourceInfo
	{
		string name;
		public string[] Dependencies { get; set; }

		public string Description { get; set; }
		public string Game { get; set; }
		public bool IsMission { get { return !string.IsNullOrEmpty(MissionScript); } }
		public string MissionScript { get; set; }
		public List<MissionSlot> MissionSlots = new List<MissionSlot>();
		public Ai[] ModAis { get; set; }
		public string Mutator { get; set; }
		public Option[] Options { get; set; }
		public string MissionMap { get; set; }

		/// <summary>
		/// Mod version as unitsync reports it
		/// </summary>
		public string PrimaryModVersion { get; set; }


		public string ShortGame { get; set; }
		public string ShortName { get; set; }
		public byte[][] SideIcons { get; set; }
		public string[] Sides { get; set; }
		public SerializableDictionary<string, string> StartUnits { get; set; }
		public UnitInfo[] UnitDefs { get; set; }

		public string GetDefaultModOptionsTags()
		{
			var builder = new StringBuilder();
			foreach (var option in Options)
			{
				var res = option.ConstructLine(option.Default);
				if (builder.Length > 0) builder.Append("\t");
				builder.Append(res);
			}
			return builder.ToString();
		}

		public static Dictionary<string, string> GetModOptionPairs(IEnumerable<string> scriptTags)
		{
			var setOptions = new Dictionary<string, string>();
			scriptTags = scriptTags.SelectMany(t => t.Split('\t')).ToArray();
			const string modOptionPattern = @"^game/modoptions/(?<key>.+?)=(?<value>.+?)$";
			foreach (var tag in scriptTags)
			{
				foreach (Match match in Regex.Matches(tag, modOptionPattern, RegexOptions.IgnoreCase))
				{
					var key = match.Groups["key"].Value;
					var value = match.Groups["value"].Value;
					setOptions[key] = value;
				}
			}
			return setOptions;
		}

		public override string ToString()
		{
			return Name;
		}

		public string ArchiveName { get; set; }

		public string Name { get { return name; } set { name = value; } }
	}
}