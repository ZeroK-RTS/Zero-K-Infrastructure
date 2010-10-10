using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace CMissionLib.UnitSyncLib
{
	[Serializable]
	public class Mod
	{
		public Ai[] AllAis { get; set; }
		string name;
		string shortBaseName;
		double version;

		public string[] Dependencies { get; set; }
		public string[] DependencyArchives { get; set; }

		public string Desctiption { get; set; }
		public string Game { get; set; }
		public string Mutator { get; set; }
		public Option[] Options { get; set; }
		public string[] PresentDependencies { get; set; }

		/// <summary>
		/// Mod version as unitsync reports it
		/// </summary>
		public string PrimaryModVersion { get; set; }

		public string ShortBaseName
		{
			get { return shortBaseName; }
		}

		public string ShortGame { get; set; }
		public string ShortName { get; set; }
		public byte[][] SideIcons { get; set; }
		public string[] Sides { get; set; }
		public Dictionary<string, string> StartUnits { get; set; }
		public UnitInfo[] UnitDefs { get; set; }

		public double Version
		{
			get { return version; }
			set { version = value; }
		}

		public string ArchiveName { get; set; }
		public int Checksum { get; set; }

		public string Name
		{
			get { return name; }
			set
			{
				name = value;
				ExtractNameAndVersion(name, out shortBaseName, out version);
			}
		}

		public Ai[] ModAis { get; set; }

		public IEnumerable<string> Files { get; set; }

		public IEnumerable<string> Widgets { get; set; }

		public IEnumerable<string> Gadgets { get; set; }

		public static void ExtractNameAndVersion(string fullName, out string name, out double version)
		{
			version = 0;
			name = fullName;
			var match = Regex.Match(fullName, "(.*[^0-9\\.]+)([0-9\\.]+)\\)*$");
			if (match.Success)
			{
				double.TryParse(match.Groups[2].Value, out version);
				name = match.Groups[1].Value;
			}
		}

		public static Dictionary<string, string> GetModOptionPairs(IEnumerable<string> scriptTags)
		{
			var setOptions = new Dictionary<string, string>();
			scriptTags = scriptTags.SelectMany(t => t.Split('\t')).ToArray();
			const string modOptionPattern = @"^game/modoptions/(?<key>.+?)=(?<value>.+?)$";
			foreach (var tag in scriptTags)
			{
				foreach (Match match in Regex.Matches(tag, modOptionPattern))
				{
					var key = match.Groups["key"].Value;
					var value = match.Groups["value"].Value;
					setOptions[key] = value;
				}
			}
			return setOptions;
		}

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

		public override string ToString()
		{
			return Name;
		}
	}
}