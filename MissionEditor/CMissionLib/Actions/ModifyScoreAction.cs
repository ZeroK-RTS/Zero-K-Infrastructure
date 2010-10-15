using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ModifyScoreAction : Action
	{
		// using enums in WCF is not a good idea because of reorderings and no default value, so use an array
		public static string[] Actions = new[] {"Increase Score", "Reduce Score", "Multiply Score", "Set Score"};
		string action = Actions[0];
		double value;

		public ModifyScoreAction()
			: base("Modify Score") {}

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

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"action", action},
					{"value", value},
				};
			return new LuaTable(map);
		}
	}
}