using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;

namespace CMissionLib.Actions
{
	[DataContract]
	public class StopCutsceneActionsAction : Action
	{
        string cutsceneID;

        public StopCutsceneActionsAction()
		{
            cutsceneID = "Current Cutscene";
		}

		[DataMember]
		public string CutsceneID
		{
            get { return cutsceneID; }
			set
			{
                cutsceneID = value;
                RaisePropertyChanged("CutsceneID");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"cutsceneID", CutsceneID},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Stop Cutscene Actions";
		}
	}
}