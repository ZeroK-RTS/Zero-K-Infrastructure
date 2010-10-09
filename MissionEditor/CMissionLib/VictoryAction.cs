using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public class VictoryAction : Action
	{
		public VictoryAction() : base("Victory")
		{
		}
		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}