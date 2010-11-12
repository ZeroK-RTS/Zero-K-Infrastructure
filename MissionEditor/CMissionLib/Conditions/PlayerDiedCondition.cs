using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class PlayerDiedCondition : Condition
	{
		Player player;

		public PlayerDiedCondition(Player player) : base()
		{
			Player = player;
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
					{"playerNumber", mission.Players.IndexOf(Player)},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Player Died";
		}
	}
}