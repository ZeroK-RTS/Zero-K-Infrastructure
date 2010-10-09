using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class DefeatAction : Action
	{
		public DefeatAction()
			: base("Defeat") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}