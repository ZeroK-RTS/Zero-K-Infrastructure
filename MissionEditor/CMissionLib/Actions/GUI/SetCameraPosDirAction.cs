using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
    /// <summary>
    /// Manually set the camera's position and rotation
    /// Any unspecified values will be left unchanged
    /// </summary>
	[DataContract]
	public class SetCameraPosDirAction : Action
	{
        double? px, py, pz;
        double? rx, ry;
        double time;

		public SetCameraPosDirAction()
            : base() { }

        [DataMember]
        public double? PX
        {
            get { return px; }
            set
            {
                px = value;
                RaisePropertyChanged("PX");
            }
        }
        [DataMember]
        public double? PY
        {
            get { return py; }
            set
            {
                py = value;
                RaisePropertyChanged("PY");
            }
        }
        [DataMember]
        public double? PZ
        {
            get { return pz; }
            set
            {
                pz = value;
                RaisePropertyChanged("PZ");
            }
        }
        [DataMember]
        public double? RX
        {
            get { return rx; }
            set
            {
                rx = value;
                RaisePropertyChanged("RX");
            }
        }
        [DataMember]
        public double? RY
        {
            get { return ry; }
            set
            {
                ry = value;
                RaisePropertyChanged("RY");
            }
        }
        /// <summary>
        /// camTime argument for <code>Spring.SetCameraState</code>
        /// </summary>
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
					{"px", PX},
					{"py", PY},
                    {"pz", PZ},
                    {"rx", RX},
                    {"ry", RY},
                    {"time", Time},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Set Camera Position/Direction";
		}
	}
}