using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
    public abstract class OrderTypeIconUnitMap : Positionable, IOrder
	{
		protected OrderTypeIconUnitMap(double x, double y) : base(x, y)
		{
		}

		public abstract string OrderType { get; }
		public abstract string Name { get; }
        public string Group { get; set; }

		public LuaTable GetLuaMap(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"orderType", OrderType},
				};
            if (!String.IsNullOrEmpty(Group)) map.Add("target", Group);
            else
            {
                var args = LuaTable.CreateArray(new[] { mission.ToIngameX(X), 0, mission.ToIngameY(Y) });
                map.Add("args", args);
            }
			return new LuaTable(map);
		}
	}
}