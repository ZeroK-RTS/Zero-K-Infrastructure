using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public class CutsceneSkippedCondition : Condition
	{
		string cutsceneID;

        public CutsceneSkippedCondition()
            : base()
		{
			CutsceneID = cutsceneID;
		}


		[DataMember]
        public String CutsceneID
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
					{"cutsceneID", cutsceneID},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Cutscene Skipped";
		}
	}
}