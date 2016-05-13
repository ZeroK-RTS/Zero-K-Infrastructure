using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ModifyCountdownAction : TimeBasedAction
	{
		public static string[] Modes = new[] {"Extend", "Anticipate"};
		string countdown;
		string mode = Modes[0];

		public ModifyCountdownAction(string countdown)
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
		public string Mode
		{
			get { return mode; }
			set
			{
				mode = value;
				RaisePropertyChanged("Mode");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"countdown", Countdown},
					{"action", Mode},
					{"frames", Frames},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Modifiy Countdown";
		}
	}
}