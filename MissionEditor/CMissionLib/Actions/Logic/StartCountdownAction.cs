using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class StartCountdownAction : TimeBasedAction
	{
		string countdown;
		bool display = true;

		public StartCountdownAction(string countdown)
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

		[DataMember]
		public bool Display
		{
			get { return display; }
			set
			{
				display = value;
				RaisePropertyChanged("Display");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"countdown", Countdown},
					{"display", Display},
					{"frames", Frames},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Start Countdown";
		}
	}
}