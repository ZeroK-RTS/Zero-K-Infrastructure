using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	/// <summary>
	/// Writes the score to infolog for Zero-K Lobby to read
	/// </summary>
	[DataContract]
	public class SendScoreAction : Action
	{
		public SendScoreAction()
			: base() {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Send Scores";
		}
	}
}