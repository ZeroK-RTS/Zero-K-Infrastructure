using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
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