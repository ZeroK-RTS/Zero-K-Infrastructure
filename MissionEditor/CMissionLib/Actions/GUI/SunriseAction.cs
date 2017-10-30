using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	/// <summary>
	/// Makes the day/night widget transition to day
	/// </summary>
	[DataContract]
	public class SunriseAction : Action
	{
		public SunriseAction()
			: base() {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Sunrise";
		}
	}
}