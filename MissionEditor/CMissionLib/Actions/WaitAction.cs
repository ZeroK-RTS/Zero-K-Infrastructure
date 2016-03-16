using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class WaitAction : TimeBasedAction
	{
		public WaitAction()
			: base() {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"frames", frames},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Wait";
		}
	}
}