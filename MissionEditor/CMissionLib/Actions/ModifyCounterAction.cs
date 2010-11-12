using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ModifyCounterAction : Action
	{
		// using enums in WCF is not a good idea because of reorderings and no default value, so use an array
		public static string[] Actions = new[] {"Increase", "Reduce", "Multiply", "Set"};

		string action = Actions[0];
		string counter = String.Empty;
		double value;

		public ModifyCounterAction()
			: base() {}

		[DataMember]
		public string Action
		{
			get { return action; }
			set
			{
				action = value;
				RaisePropertyChanged("Action");
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
					{"action", action},
					{"value", value},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Modify Counter";
		}
	}
}