using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
    [DataContract]
    public class TimeBasedAction : Action, ITimeSpan
    {
        //protected static List<String> properties = new List<string>() { "Seconds", "Minutes", "Frames", "Time" };
        protected int frames;
        
        public double Seconds
        {
            get { return frames / 30.0; }
            set
            {
                frames = (int)(value * 30);
                RaiseTimeChanged();
            }
        }
        
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

        public override string GetDefaultName()
        {
            return "Time-based Action";
        }

        public override LuaTable GetLuaTable(Mission mission)
        {
            return new LuaTable();
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