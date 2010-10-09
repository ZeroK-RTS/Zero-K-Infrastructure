using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class CustomCondition : Condition
	{
		public CustomCondition()
			: base("Custom Condition") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}