using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class CounterModifiedCondition : Condition
	{
		// using enums in WCF is not a good idea because of reorderings and no default value, so use an array
		public static string[] Conditions = new[] {">", "=", ">=", "<", "<=", "!="};
		string condition = Conditions[0];
		string counter = String.Empty;
		double value;

		public CounterModifiedCondition()
			: base("Counter Modified") {}

		[DataMember]
		public string Condition
		{
			get { return condition; }
			set
			{
				condition = value;
				RaisePropertyChanged("Condition");
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

		[DataMember]
		public string Counter
		{
			get { return counter; }
			set
			{
				counter = value;
				RaisePropertyChanged("Counter");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"counter", counter},
					{"condition", condition},
					{"value", value},
				};
			return new LuaTable(map);
		}
	}
}