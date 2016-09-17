using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
    /// <summary>
    /// Writes the status of all countdowns to the console
    /// </summary>
	[DataContract]
	public class DisplayCountersAction : Action
	{
		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Display Counters";
		}
	}
}