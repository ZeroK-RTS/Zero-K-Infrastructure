using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class GameStartedCondition : Condition
	{
		public GameStartedCondition()
			: base("Game Starts") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}