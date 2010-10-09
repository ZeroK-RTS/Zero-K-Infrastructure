using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class UnitFinishedCondition : Condition
	{
		public UnitFinishedCondition()
			: base("Unit Finished")
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
			var map = new Dictionary<string, object>
				{
					{"units", new LuaTable(Units)},
					{"players", new LuaTable(Players.Select(p => mission.Players.IndexOf(p)).Cast<object>())},
				};
			return new LuaTable(map);
		}
	}
}