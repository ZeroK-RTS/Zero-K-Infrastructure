using System;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class TimeBasedCondition : Condition, ITimeSpan
	{
		public TimeBasedCondition() : base() {}

        protected int frames;

        [DataMember]
        public double Seconds
        {
            get { return frames / 30.0; }
            set
            {
                frames = (int)(value * 30);
                RaiseTimeChanged();
            }
        }

        [DataMember]
        public double Minutes
        {
            get { return frames / 30.0 / 60; }
            set
            {
                frames = (int)(value * 30 * 60);
                RaiseTimeChanged();
            }
        }

        [DataMember]
        public int Frames
        {
            get { return frames; }
            set
            {
                frames = value;
                RaiseTimeChanged();
            }
        }

        public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Time-Based Condition";
		}

        protected void RaiseTimeChanged()
        {
            RaisePropertyChanged("Frames");
            RaisePropertyChanged("Seconds");
            RaisePropertyChanged("Time");
            RaisePropertyChanged("Minutes");
        }
    }
}