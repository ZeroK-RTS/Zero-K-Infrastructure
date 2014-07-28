using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class UnitEnteredLOSCondition : Condition
	{
		ObservableCollection<string> groups = new ObservableCollection<string>();
		ObservableCollection<string> alliances = new ObservableCollection<string>();

        public UnitEnteredLOSCondition() : base() { }

		[DataMember]
		public ObservableCollection<string> Alliances
		{
			get { return alliances; }
			set
			{
				alliances = value;
				RaisePropertyChanged("Alliances");
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
            var missionAlliances = mission.Alliances.Distinct().ToList();
			var map = new Dictionary<object, object>
				{
					{"alliances", LuaTable.CreateArray(Alliances.Select(a => missionAlliances.IndexOf(a)))},
					{"groups", LuaTable.CreateSet(groups)},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Unit Entered LOS";
		}
	}
}
