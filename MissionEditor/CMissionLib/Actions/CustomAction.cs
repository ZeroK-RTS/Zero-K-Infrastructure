using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class CustomAction : Action
	{
		public CustomAction()
			: base("Custom Action") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}