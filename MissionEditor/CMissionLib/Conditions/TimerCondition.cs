using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class TimerCondition: TimeBasedCondition
	{
		public override string GetDefaultName()
		{
			return "Time Elapsed";
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object> { { "frames", Frames }, };
			return new LuaTable(map);
		}
	}
}