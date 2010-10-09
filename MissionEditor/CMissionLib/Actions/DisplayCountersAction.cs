using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class DisplayCountersAction : Action
	{
		public DisplayCountersAction()
			: base("Display Counters") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}