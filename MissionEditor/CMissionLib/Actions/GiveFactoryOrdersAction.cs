using System;
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
		public GiveFactoryOrdersAction() : base("Give Factory Orders") {}

		[DataMember]
		public ObservableCollection<string> FactoryGroups
		{
			get { return factoryGroups; }
			set
			{
				factoryGroups = value;
				RaisePropertyChanged("factoryGroups");
			}
		}

		[DataMember]
		public ObservableCollection<string> BuiltUnitsGroups
		{
			get { return builtUnitsGroups; }
			set
			{
				builtUnitsGroups = value;
				RaisePropertyChanged("builtUnitsGroups");
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
			throw new NotImplementedException();
		}
	}
}