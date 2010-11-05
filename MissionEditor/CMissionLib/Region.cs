using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib
{
	[DataContract]
	public class Region : PropertyChanged
	{
		string name;
		ObservableCollection<Area> areas = new ObservableCollection<Area>();

		[DataMember]
		public ObservableCollection<Area> Areas
		{
			get { return areas; }
			set
			{
				areas = value;
				RaisePropertyChanged("Areas");
			}
		}

		[DataMember]
		public string Name
		{
			get { return name; }
			set 
			{ 
				name = value;
				RaisePropertyChanged("Name");
			}
		}

		public LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"areas", LuaTable.CreateArray(areas.Select(a => a.GetLuaMap(mission)))},
					{"name", Name},
				};
			return new LuaTable(map);
		}
	}


}
