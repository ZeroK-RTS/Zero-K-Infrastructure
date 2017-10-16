using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	/// <summary>
	/// Ends the game in defeat for the player
	/// All allyTeams without a human player on them are marked as winners
	/// </summary>
	[DataContract]
	public class DefeatAction : Action
	{

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Defeat";
		}
	}
}