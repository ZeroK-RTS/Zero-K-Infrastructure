using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class SunriseAction : Action
	{
		public SunriseAction()
			: base("Sunrise") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}