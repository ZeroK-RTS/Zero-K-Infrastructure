using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class SoundAction : Action
	{
		string soundPath;

		public SoundAction()
			: base() {}

        [DataMember]
		public string SoundPath
		{
			get { return soundPath; }
			set
			{
				soundPath = value;
				RaisePropertyChanged("SoundPath");
			}
		}


		public override LuaTable GetLuaTable(Mission mission)
		{
            var map = new Dictionary<object, object> { };
            if (File.Exists(SoundPath))
            {
                map.Add("sound", Path.GetFileName(SoundPath));
            }
            else if (!String.IsNullOrWhiteSpace(SoundPath))
            {
                map.Add("sound", SoundPath);
                map.Add("soundFromArchive", true);
            }
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Play Sound";
		}
	}
}