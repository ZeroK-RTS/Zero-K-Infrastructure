using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class SendScoreAction : Action
	{
		public SendScoreAction()
			: base("Send Score") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}