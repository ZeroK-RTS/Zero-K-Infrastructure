using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class UnitDestroyedCondition : Condition
	{
		ObservableCollection<string> groups = new ObservableCollection<string>();

		public UnitDestroyedCondition()
			: base("Unit Destroyed") {}

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