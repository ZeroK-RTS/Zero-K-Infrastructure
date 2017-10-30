using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	/// <summary>
	/// Clears all queued convo messages (including the one currently being displayed)
	/// <seealso cref="ConvoMessageAction"/>
	/// </summary>
	[DataContract]
	public class ClearConvoMessageQueueAction : Action
	{

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
            return "Clear Convo Message Queue";
		}
	}
}