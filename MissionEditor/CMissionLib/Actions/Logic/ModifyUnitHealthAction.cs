using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
    /// <summary>
    /// Inflicts the specified amount of damage on all members of the specified unit group 
    /// (can be negative)
    /// </summary>
	[DataContract]
	public class ModifyUnitHealthAction : Action
	{
		double damage;
		string group = String.Empty;

		public ModifyUnitHealthAction()
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

		public override string GetDefaultName()
		{
			return "Modify Unit Health";
		}
	}
}