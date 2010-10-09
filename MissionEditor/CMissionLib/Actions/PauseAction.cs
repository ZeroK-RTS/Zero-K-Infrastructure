using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class PauseAction : Action
	{
		public PauseAction()
			: base("Pause") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}