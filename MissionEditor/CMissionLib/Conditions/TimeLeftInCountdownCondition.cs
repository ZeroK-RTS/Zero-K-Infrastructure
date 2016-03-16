using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class TimeLeftInCountdownCondition : TimeBasedCondition
	{
		string countdown;

		public TimeLeftInCountdownCondition(string countdown)
			: base()
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
					{"countdown", Countdown},
					{"frames", Frames},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Time Left in Countdown";
		}
	}
}