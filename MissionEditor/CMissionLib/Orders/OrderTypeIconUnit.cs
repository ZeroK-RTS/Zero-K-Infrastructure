using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
    public abstract class OrderTypeIconUnit : IOrder
	{
		protected OrderTypeIconUnit() 
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
                    {"target", Group}
				};
			return new LuaTable(map);
		}
	}
}