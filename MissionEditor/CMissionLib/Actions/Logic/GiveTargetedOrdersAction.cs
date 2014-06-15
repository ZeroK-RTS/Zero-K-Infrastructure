using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class GiveTargetedOrdersAction : Action
	{
		ObservableCollection<string> groups = new ObservableCollection<string>();
        ObservableCollection<string> targetGroups = new ObservableCollection<string>();
		ObservableCollection<IOrder> orders;
        bool queue = false;

		public GiveTargetedOrdersAction()
			: this(new ObservableCollection<IOrder>()) {}

		public GiveTargetedOrdersAction(IEnumerable<IOrder> orders)
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

        [DataMember]
        public ObservableCollection<string> TargetGroups
        {
            get { return targetGroups; }
            set
            {
                targetGroups = value;
                RaisePropertyChanged("TargetGroups");
            }
        }

        [DataMember]
        public bool Queue
        {
            get { return queue; }
            set
            {
                queue = value;
                RaisePropertyChanged("Queue");
            }
        }

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"orders", LuaTable.CreateArray(orders.Select(o => o.GetLuaMap(mission)).ToArray())},
					{"groups", LuaTable.CreateSet(groups)},
                    {"targetGroups", targetGroups},
                    {"queue", queue}
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Give Targeted Orders";
		}
	}
}