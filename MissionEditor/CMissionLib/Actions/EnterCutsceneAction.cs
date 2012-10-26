using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class LeaveCutsceneAction : Action
	{
		public LeaveCutsceneAction()
			: base() {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Leave Cutscene";
		}
	}
}