using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ModifyUnitHealthAction : Action
	{
		double damage;
		string group = String.Empty;

		public ModifyUnitHealthAction()
			: base("Modify Unit Health") {}

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

		[DataMember]
		public double Damage
		{
			get { return damage; }
			set
			{
				damage = value;
				RaisePropertyChanged("Damage");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"damage", damage},
					{"group", group},
				};
			return new LuaTable(map);
		}
	}
}