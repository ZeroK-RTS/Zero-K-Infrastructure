using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;

namespace CMissionLib.Actions
{
    /// <summary>
    /// Cancels all remaining actions specified in a trigger between the <see cref="EnterCutsceneAction"/> and the <see cref="LeaveCutsceneAction"/> or the end of the trigger
    /// If an already-executed action executes a trigger, actions in that trigger will not be affected
    /// </summary>
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