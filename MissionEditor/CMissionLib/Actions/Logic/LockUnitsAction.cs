using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class LockUnitsAction : Action
	{
		public LockUnitsAction()
		{
			Units = new ObservableCollection<string>();
		}

		[DataMember]
		public ObservableCollection<string> Units { get; set; }

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"units", LuaTable.CreateArray(Units)},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Lock Units";
		}
	}
}