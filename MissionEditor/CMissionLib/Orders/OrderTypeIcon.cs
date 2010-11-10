using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public abstract class OrderTypeIcon : IOrder
	{
		public abstract string OrderType { get; }
		public abstract string Name { get; }
		public LuaTable GetLuaMap(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"orderType", OrderType},
					{"args", LuaTable.Empty},
				};
			return new LuaTable(map);
		}
	}
}