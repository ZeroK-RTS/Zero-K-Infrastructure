using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class SetCameraUnitTargetAction : Action
	{
		public SetCameraUnitTargetAction()
			: base("Unit Camera")
		{
			Group = String.Empty;
		}

		[DataMember]
		public string Group { get; set; }


		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<string, object>
				{
					{"group", Group},
				};
			return new LuaTable(map);
		}
	}
}