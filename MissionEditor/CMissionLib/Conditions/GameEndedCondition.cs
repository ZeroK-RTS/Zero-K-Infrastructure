using System;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class GameEndedCondition : Condition
	{
		public GameEndedCondition()
			: base() {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Game Ends";
		}
	}
}