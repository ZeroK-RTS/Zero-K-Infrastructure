using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
    /// <summary>
    /// Makes the day/night widget transition to night
    /// </summary>
	[DataContract]
	public class SunsetAction : Action
	{
		public SunsetAction()
			: base() {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Sunset";
		}
	}
}