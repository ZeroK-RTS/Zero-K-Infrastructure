using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class StartCountdownAction : Action
	{
		string countdown;
		bool display = true;
		TimeSpan time;

		public StartCountdownAction(string countdown)
			: base("Start Countdown")
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
		public bool Display
		{
			get { return display; }
			set
			{
				display = value;
				RaisePropertyChanged("Display");
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
			var map = new Dictionary<string, object>
				{
					{"countdown", Countdown},
					{"display", Display},
					{"frames", Frames},
				};
			return new LuaTable(map);
		}
	}
}