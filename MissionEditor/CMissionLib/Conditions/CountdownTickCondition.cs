using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class CountdownTickCondition : Condition
	{
		string countdown;
		TimeSpan time;

		public CountdownTickCondition(string countdown)
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

		public double Seconds
		{
			get { return time.TotalSeconds; }
			set
			{
				time = TimeSpan.FromSeconds(value);
				RaiseTimeChanged();
			}
		}

		public double Minutes
		{
			get { return time.TotalMinutes; }
			set
			{
				time = TimeSpan.FromMinutes(value);
				RaiseTimeChanged();
			}
		}


		public double Frames // 30 gameframes per second
		{
			get { return time.TotalSeconds*30; }
			set
			{
				time = TimeSpan.FromSeconds(value/30);
				RaiseTimeChanged();
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
					{"countdown",  Countdown??string.Empty},
					{"frames", Math.Floor(Frames)},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Countdown Ticks";
		}
	}
}