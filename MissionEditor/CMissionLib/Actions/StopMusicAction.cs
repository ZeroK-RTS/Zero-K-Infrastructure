using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	/// <summary>
	/// Stops the currently playing music
	/// </summary>
	[DataContract]
	public class StopMusicAction : Action
	{
		bool noContinue = true;

		/// <summary>
		/// If true, music player widget will not auto-play the next track
		/// </summary>
		[DataMember]
		public bool NoContinue
		{
			get { return noContinue; }
			set
			{
                noContinue = value;
				RaisePropertyChanged("NoContinue");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"noContinue", noContinue},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Stop Music";
		}
	}
}