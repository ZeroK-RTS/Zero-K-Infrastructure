using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ModifyScoreAction : Action
	{
		// using enums in WCF is not a good idea because of reorderings and no default value, so use an array
		public static string[] Actions = new[] {"Increase Score", "Reduce Score", "Multiply Score", "Set Score"};
		string action = Actions[0];
		double value;
		ObservableCollection<Player> players = new ObservableCollection<Player>();

		public ModifyScoreAction()
			: base() {}

		[DataMember]
		public string Action
		{
			get { return action; }
			set
			{
				action = value;
				RaisePropertyChanged("Action");
			}
		}

		[DataMember]
		public double Value
		{
			get { return value; }
			set
			{
				this.value = value;
				RaisePropertyChanged("Value");
			}
		}

		[DataMember]
		public ObservableCollection<Player> Players
		{
			get { return players; }
			set
			{
				players = value;
				RaisePropertyChanged("Players");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"action", action},
					{"value", value},
					{"players", LuaTable.CreateArray(players.Select(p => mission.Players.IndexOf(p)))},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Modify Score";
		}
	}
}