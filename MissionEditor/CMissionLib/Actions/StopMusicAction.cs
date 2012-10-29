using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class StopMusicAction : Action
	{
		bool noContinue = true;

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