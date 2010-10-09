using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class DummyAction : Action
	{
		public DummyAction() : base("Dummy") {}


		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}