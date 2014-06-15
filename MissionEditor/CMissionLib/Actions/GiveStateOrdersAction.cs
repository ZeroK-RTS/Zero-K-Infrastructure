using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class GiveStateOrdersAction : Action
	{
		string group = String.Empty;

		public GiveStateOrdersAction()
			: base() {}

		[DataMember]
		public string Group
		{
			get { return group; }
			set
			{
				group = value;
				RaisePropertyChanged("Group");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"group", group},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Give State Orders";
		}
	}
}