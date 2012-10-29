using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class MusicAction : Action
	{
		string trackPath;

		public MusicAction()
			: base() {}

        [DataMember]
		public string TrackPath
		{
			get { return trackPath; }
			set
			{
				trackPath = value;
				RaisePropertyChanged("TrackPath");
			}
		}


		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>{};
            if (File.Exists(TrackPath)) 
            {
                map.Add("track", Path.GetFileName(TrackPath));
            }
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Play Music";
		}
	}
}