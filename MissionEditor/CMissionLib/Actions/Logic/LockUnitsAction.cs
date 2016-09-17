using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Linq;

namespace CMissionLib.Actions
{
    /// <summary>
    /// Make certain units unbuildable by the specified teams
    /// <seealso cref="UnlockUnitsAction"/>
    /// </summary>
	[DataContract]
	public class LockUnitsAction : Action
	{
		public LockUnitsAction()
		{
            Players = new ObservableCollection<Player>();
			Units = new ObservableCollection<string>();
		}

		[DataMember]
		public ObservableCollection<string> Units { get; set; }

        /// <summary>
        /// Teams for whom the unit will be locked; empty set means all teams are affected
        /// </summary>
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

		public override string GetDefaultName()
		{
			return "Lock Units";
		}
	}
}