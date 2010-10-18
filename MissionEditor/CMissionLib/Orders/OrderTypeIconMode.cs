using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public abstract class OrderTypeIconMode : IOrder
	{
		public OrderTypeIconMode(int mode)
		{
			Mode = mode;
		}

		[DataMember]
		public int Mode { get; set; }

		public abstract string OrderType { get; }
		public abstract string Name { get; }

		public LuaTable GetLuaMap(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"orderType", OrderType},
					{"args", LuaTable.CreateArray(new [] { Mode })},
				};
			return new LuaTable(map);
		}
	}
}