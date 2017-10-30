using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	/// <summary>
	/// Calls the custom action specified in mission_runner.lua
	/// (by default, it does nothing, mission needs to include its own mission runner)
	/// Not recommended, use <see cref="CustomAction2"/> instead
	/// </summary>
	[Obsolete("Use CustomAction2 instead")]
	[DataContract]
	public class CustomAction : Action
	{
		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Custom Action (Legacy)";
		}
	}
}