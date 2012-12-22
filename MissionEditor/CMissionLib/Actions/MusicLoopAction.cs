using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class MusicLoopAction : Action
	{
		string trackIntroPath, trackLoopPath;

		public MusicLoopAction()
			: base() {}

        [DataMember]
		public string TrackIntroPath
		{
			get { return trackIntroPath; }
			set
			{
				trackIntroPath = value;
				RaisePropertyChanged("TrackIntroPath");
			}
		}

        [DataMember]
        public string TrackLoopPath
        {
            get { return trackLoopPath; }
            set
            {
                trackLoopPath = value;
                RaisePropertyChanged("TrackLoopPath");
            }
        }

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>{};
            if (File.Exists(TrackIntroPath) && File.Exists(TrackIntroPath)) 
            {
                map.Add("trackIntro", Path.GetFileName(TrackIntroPath));
                map.Add("trackLoop", Path.GetFileName(TrackLoopPath));
            }
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Play Looping Music";
		}
	}
}