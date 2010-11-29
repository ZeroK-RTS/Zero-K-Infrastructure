using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ModifyResourcesAction : Action
	{
		public static string[] Categories = new[] {"metal", "energy", "metal storage", "energy storage"};
		double amount;
		string category = Categories[0];
		Player player;

		public ModifyResourcesAction(Player player)
			: base()
		{
			this.Player = player;
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

		[DataMember]
		public string Category
		{
			get { return category; }
			set
			{
				category = value;
				RaisePropertyChanged("Category");
			}
		}

		[DataMember]
		public double Amount
		{
			get { return amount; }
			set
			{
				amount = value;
				RaisePropertyChanged("Amount");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"amount", Amount},
					{"player", mission.Players.IndexOf(player)},
					{"category", Category},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Modify Resources";
		}
	}
}