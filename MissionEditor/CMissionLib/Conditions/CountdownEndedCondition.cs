using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class CountdownEndedCondition : Condition
	{
		string countdown;

		public CountdownEndedCondition(string countdown)
			: base("Countdown Ended")
		{
			this.countdown = countdown;
		}

		[DataMember]
		public string Countdown
		{
			get { return countdown; }
			set
			{
				countdown = value;
				RaisePropertyChanged("Countdown");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"countdown",  Countdown??string.Empty},
				};
			return new LuaTable(map);
		}
	}
}