using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class UnitBuiltOnGhostCondition : Condition
	{

		ObservableCollection<string> groups = new ObservableCollection<string>();

		public UnitBuiltOnGhostCondition(): base("Unit Built On Ghost") {}

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
					{"groups", LuaTable.CreateSet(groups)},
				};
			return new LuaTable(map);
		}
	}
}
