using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class GameEndedCondition : Condition
	{
		public GameEndedCondition()
			: base("Game Ends") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}