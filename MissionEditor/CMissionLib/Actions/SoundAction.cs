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
			: base("Play Sound") {}

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
			var map = new Dictionary<object, object>
				{
					{"sound", Path.GetFileName(SoundPath)},
				};
			return new LuaTable(map);
		}
	}
}