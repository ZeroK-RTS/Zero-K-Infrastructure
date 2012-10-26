using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class EnterCutsceneAction : Action
	{
		public EnterCutsceneAction()
			: base() {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Enter Cutscene";
		}
	}
}