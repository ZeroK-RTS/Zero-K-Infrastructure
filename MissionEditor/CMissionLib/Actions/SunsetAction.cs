using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class SunsetAction : Action
	{
		public SunsetAction()
			: base("Sunset") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}