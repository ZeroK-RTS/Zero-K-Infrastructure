using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class SaveCameraStateAction : Action
	{

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Save Camera State";
		}
	}
}