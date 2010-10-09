using System;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class DummyCondition : Condition
	{
		public DummyCondition() : base("Dummy") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}