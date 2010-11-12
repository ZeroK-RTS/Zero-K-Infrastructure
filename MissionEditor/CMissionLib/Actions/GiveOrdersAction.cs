using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class GiveOrdersAction : Action
	{
		ObservableCollection<string> groups = new ObservableCollection<string>();
		ObservableCollection<IOrder> orders;

		public GiveOrdersAction()
			: this(new ObservableCollection<IOrder>()) {}

		public GiveOrdersAction(IEnumerable<IOrder> orders)
		{
			this.orders = new ObservableCollection<IOrder>(orders);
		}

		[DataMember]
		public ObservableCollection<IOrder> Orders
		{
			get { return orders; }
			set
			{
				orders = value;
				RaisePropertyChanged("Orders");
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
					{"orders", LuaTable.CreateArray(orders.Select(o => o.GetLuaMap(mission)).ToArray())},
					{"groups", LuaTable.CreateSet(groups)}
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Give Orders";
		}
	}
}