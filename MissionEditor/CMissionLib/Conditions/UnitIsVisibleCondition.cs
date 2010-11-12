using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class UnitIsVisibleCondition : Condition
	{
		ObservableCollection<string> groups = new ObservableCollection<string>();
		ObservableCollection<Player> players = new ObservableCollection<Player>();

		public UnitIsVisibleCondition() : base() { }

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

		[DataMember]
		public ObservableCollection<string> Groups
		{
			get { return groups; }
			set
			{
				groups = value;
				RaisePropertyChanged("Groups");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"players", LuaTable.CreateArray(Players.Select(p => mission.Players.IndexOf(p)))},
					{"groups", LuaTable.CreateSet(groups)},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Unit Is Visible";
		}
	}
}
