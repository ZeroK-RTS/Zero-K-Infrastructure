using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class UnitFinishedInFactoryCondition : Condition
	{
		public UnitFinishedInFactoryCondition()
			: base("Unit Finished In Factory")
		{
			Players = new ObservableCollection<Player>();
			Units = new ObservableCollection<string>();
		}

		[DataMember]
		public ObservableCollection<string> Units { get; set; }

		[DataMember]
		public ObservableCollection<Player> Players { get; set; }

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"units", LuaTable.CreateArray(Units)},
					{"players", LuaTable.CreateArray(Players.Select(p => mission.Players.IndexOf(p)))},
				};
			return new LuaTable(map);
		}
	}
}
