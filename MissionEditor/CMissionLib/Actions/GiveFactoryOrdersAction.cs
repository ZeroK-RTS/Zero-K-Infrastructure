using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class GiveFactoryOrdersAction : Action
	{
		ObservableCollection<string> buildOrders = new ObservableCollection<string>();
		ObservableCollection<string> builtUnitsGroups = new ObservableCollection<string>();
		ObservableCollection<string> factoryGroups = new ObservableCollection<string>();
		bool repeat;

		[DataMember]
		public ObservableCollection<string> FactoryGroups
		{
			get { return factoryGroups; }
			set
			{
				factoryGroups = value;
				RaisePropertyChanged("FactoryGroups");
			}
		}

		[DataMember]
		public ObservableCollection<string> BuiltUnitsGroups
		{
			get { return builtUnitsGroups; }
			set
			{
				builtUnitsGroups = value;
				RaisePropertyChanged("BuiltUnitsGroups");
			}
		}

		[DataMember]
		public ObservableCollection<string> BuildOrders
		{
			get { return buildOrders; }
			set
			{
				buildOrders = value;
				RaisePropertyChanged("BuildOrders");
			}
		}

		[DataMember]
		public bool Repeat
		{
			get { return repeat; }
			set
			{
				repeat = value;
				RaisePropertyChanged("Repeat");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"buildOrders", LuaTable.CreateArray(buildOrders)},
					{"builtUnitsGroups", LuaTable.CreateSet(builtUnitsGroups)},
					{"factoryGroups", LuaTable.CreateSet(factoryGroups)},
					{"repeatOrders", repeat},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Give Factory Orders";
		}
	}
}