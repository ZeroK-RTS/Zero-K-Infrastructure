using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class TransferUnitsAction : Action
	{
		string group = String.Empty;
		Player player;

		public TransferUnitsAction(Player player)
			: base("Transfer Units")
		{
			this.player = player;
		}

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
		public Player Player
		{
			get { return player; }
			set
			{
				player = value;
				RaisePropertyChanged("Player");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"group", group},
					{"player", mission.Players.IndexOf(player)},
				};
			return new LuaTable(map);
		}
	}
}