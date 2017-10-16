using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	/// <summary>
	/// Remove or destroy the members of the selected unit group
	/// </summary>
	[DataContract]
	public class DestroyUnitsAction : Action
	{
		bool explode = true;
		string group = String.Empty;

		[DataMember]
		public string Group
		{
			get { return group; }
			set
			{
				group = value;
				RaisePropertyChanged("Group");
			}
		}

		/// <summary>
		/// If true, units are self-destructed; else they just disappear
		/// </summary>
		[DataMember]
		public bool Explode
		{
			get { return explode; }
			set
			{
				explode = value;
				RaisePropertyChanged("Explode");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"group", group},
					{"explode", explode},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Destroy Units";
		}
	}
}