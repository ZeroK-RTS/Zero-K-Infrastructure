using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public abstract class OrderTypeIconMap : Positionable, IOrder
	{
		protected OrderTypeIconMap(double x, double y) : base(x, y)
		{
		}

		public abstract string OrderType { get; }
		public abstract string Name { get; }

		public LuaTable GetLuaMap(Mission mission)
		{
			var args = LuaTable.CreateArray(new [] { mission.ToIngameX(X), 0, mission.ToIngameY(Y) });
			var map = new Dictionary<object, object>
				{
					{"orderType", OrderType},
					{"args", args},
				};
			return new LuaTable(map);
		}
	}
}