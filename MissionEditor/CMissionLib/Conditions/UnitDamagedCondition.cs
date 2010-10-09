using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class UnitDamagedCondition : Condition
	{
		bool anyAttacker = true;
		bool anyVictim = true;
		string attackerGroup = String.Empty;
		double value = 1000000;
		string victimGroup = String.Empty;

		public UnitDamagedCondition()
			: base("Unit Damaged") {}


		[DataMember]
		public string AttackerGroup
		{
			get { return attackerGroup; }
			set
			{
				attackerGroup = value;
				RaisePropertyChanged("AttackerGroup");
			}
		}

		[DataMember]
		public string VictimGroup
		{
			get { return victimGroup; }
			set
			{
				victimGroup = value;
				RaisePropertyChanged("VictimGroup");
			}
		}

		[DataMember]
		public bool AnyAttacker
		{
			get { return anyAttacker; }
			set
			{
				anyAttacker = value;
				RaisePropertyChanged("AnyAttacker");
			}
		}

		[DataMember]
		public bool AnyVictim
		{
			get { return anyVictim; }
			set
			{
				anyVictim = value;
				RaisePropertyChanged("AnyVictim");
			}
		}

		[DataMember]
		public double Value
		{
			get { return value; }
			set
			{
				this.value = value;
				RaisePropertyChanged("Value");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<string, object>
				{
					{"attackerGroup", attackerGroup},
					{"anyAttacker", anyAttacker},
					{"victimGroup", victimGroup},
					{"anyVictim", anyVictim},
					{"value", value},
				};
			return new LuaTable(map);
		}
	}
}