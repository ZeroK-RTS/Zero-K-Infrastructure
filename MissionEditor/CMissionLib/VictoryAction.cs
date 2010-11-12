using System;
using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public class VictoryAction : Action
	{
		public VictoryAction() : base()
		{
		}
		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Victory";
		}
	}
}