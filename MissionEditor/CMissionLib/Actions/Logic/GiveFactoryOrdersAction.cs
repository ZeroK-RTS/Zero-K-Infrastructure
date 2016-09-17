using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
    /// <summary>
    /// Gives build orders to the specified factory
    /// </summary>
	[DataContract]
	public class GiveFactoryOrdersAction : Action
	{
		ObservableCollection<string> buildOrders = new ObservableCollection<string>();
		ObservableCollection<string> builtUnitsGroups = new ObservableCollection<string>();
		ObservableCollection<string> factoryGroups = new ObservableCollection<string>();
		bool repeat;

        /// <summary>
        /// The unit groups of the factories to be ordered
        /// </summary>
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

        /// <summary>
        /// Assign units made by the factories to these groups
        /// <para>If factory repeat is off, only add the units ordered in this action to groups</para>
        /// <para>Else, it adds all units built by the factory to the groups</para>
        /// </summary>
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

        /// <summary>
        /// Units to build
        /// </summary>
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