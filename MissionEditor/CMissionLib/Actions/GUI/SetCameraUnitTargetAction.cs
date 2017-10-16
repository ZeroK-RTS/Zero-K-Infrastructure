using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	/// <summary>
	/// Points the camera at the first visible member of the specified unit group it finds
	/// Does nothing if no group members are in the player's LOS
	/// </summary>
	[DataContract]
	public class SetCameraUnitTargetAction : Action
	{
		public SetCameraUnitTargetAction()
			: base()
		{
			Group = String.Empty;
		}

		[DataMember]
		public string Group { get; set; }


		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"group", Group},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Unit Camera";
		}
	}
}