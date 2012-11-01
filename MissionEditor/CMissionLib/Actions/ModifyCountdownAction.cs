using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ModifyCountdownAction : Action
	{
		public static string[] Modes = new[] {"Extend", "Anticipate"};
		string countdown;
		string mode = Modes[0];
		TimeSpan time;

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
		public TimeSpan Time
		{
			get { return time; }
			set
			{
				time = value;
				RaiseTimeChanged();
			}
		}

		[DataMember]
		public double Seconds
		{
			get { return time.TotalSeconds; }
			set
			{
				time = TimeSpan.FromSeconds(value);
				RaiseTimeChanged();
			}
		}

		[DataMember]
		public double Minutes
		{
			get { return time.TotalMinutes; }
			set
			{
				time = TimeSpan.FromMinutes(value);
				RaiseTimeChanged();
			}
		}


		[DataMember]
		public double Frames // 30 gameframes per second
		{
			get { return time.TotalSeconds*30; }
			set
			{
				time = TimeSpan.FromSeconds(value/30);
				RaiseTimeChanged();
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

		void RaiseTimeChanged()
		{
			RaisePropertyChanged("Seconds");
			RaisePropertyChanged("Time");
			RaisePropertyChanged("Minutes");
			RaisePropertyChanged("Frames");
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"countdown", Countdown},
					{"action", Mode},
					{"frames", Math.Floor(Frames)},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Modifiy Countdown";
		}
	}
}