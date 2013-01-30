using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class BeautyShotAction : Action
	{
        double maxCamOffset = 10, minHeading = 15, maxHeading = 165, minPitch = -10, maxPitch = 15, minDistance = 150, maxDistance = 200;
        double time = 0;
        bool mirror = true;

		public BeautyShotAction()
			: base()
		{
			Group = String.Empty;
		}

		[DataMember]
		public string Group { get; set; }

        [DataMember]
        public double MaxCamOffset
        {
            get { return maxCamOffset; }
            set
            {
                maxCamOffset = value;
                RaisePropertyChanged("MaxCamOffset");
            }
        }
        [DataMember]
        public double MinHeading
        {
            get { return minHeading; }
            set
            {
                minHeading = value;
                RaisePropertyChanged("MinHeading");
            }
        }
        [DataMember]
        public double MaxHeading
        {
            get { return maxHeading; }
            set
            {
                maxHeading = value;
                RaisePropertyChanged("MaxHeading");
            }
        }
        [DataMember]
        public double MinPitch
        {
            get { return minPitch; }
            set
            {
                minPitch = value;
                RaisePropertyChanged("MinPitch");
            }
        }
        [DataMember]
        public double MaxPitch
        {
            get { return maxPitch; }
            set
            {
                maxPitch = value;
                RaisePropertyChanged("MaxPitch");
            }
        }
        [DataMember]
        public double MinDistance
        {
            get { return minDistance; }
            set
            {
                minDistance = value;
                RaisePropertyChanged("MinDistance");
            }
        }
        [DataMember]
        public double MaxDistance
        {
            get { return maxDistance; }
            set
            {
                maxDistance = value;
                RaisePropertyChanged("MaxDistance");
            }
        }
        [DataMember]
        public double Time
        {
            get { return time; }
            set
            {
                time = value;
                RaisePropertyChanged("Time");
            }
        }

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
                    {"group", Group},
					{"maxCamOffset", MaxCamOffset},
					{"minHeading", MinHeading},
                    {"maxHeading", MaxHeading},
                    {"minPitch", MinPitch},
                    {"maxPitch", MaxPitch},
                    {"time", Time},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Beauty Shot";
		}
	}
}