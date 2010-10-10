using System;
using System.Linq;

namespace CMissionLib.UnitSyncLib
{
	[Serializable]
	public class Ai
	{
		public Option[] Options { get; set; }
		public AiInfoPair[] Info { get; set; }

		public bool IsLuaAi
		{
			get
			{
				var pair = Info.SingleOrDefault(i => i.Key == "name");
				return pair != null && pair.Description.Contains("Lua");
			}
		}


		public string Description
		{
			get
			{
				var pair = Info.SingleOrDefault(i => i.Key == "description");
				return pair != null ? pair.Value : String.Empty;
			}
		}

		public string Name
		{
			get
			{
				var pair = Info.SingleOrDefault(i => i.Key == "name");
				return pair != null ? pair.Value : String.Empty;
			}
		}

		public string ShortName
		{
			get
			{
				var pair = Info.SingleOrDefault(i => i.Key == "shortName");
				return pair != null ? pair.Value : String.Empty;
			}
		}

		public string Version
		{
			get
			{
				var pair = Info.SingleOrDefault(i => i.Key == "version");
				return pair != null ? pair.Value : String.Empty;
			}
		}

		public string Url
		{
			get
			{
				var pair = Info.SingleOrDefault(i => i.Key == "url");
				return pair != null ? pair.Value : String.Empty;
			}
		}

		public override string ToString()
		{
			return Name;
		}
	}
}