using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
    /// <summary>
    /// Enters a cutscene with the specified ID
    /// </summary>
	[DataContract]
	public class EnterCutsceneAction : Action
	{
        bool instant = false;
        bool skippable = false;
        string id;

		public EnterCutsceneAction()
			: base() {}

        /// <summary>
        /// If true, the letterbox effect reaches its full extent immediately 
        /// instead of scrolling in from the top/bottom of the screen
        /// </summary>
        [DataMember]
        public bool Instant
        {
            get { return instant; }
            set
            {
                instant = value;
                RaisePropertyChanged("Instant");
            }
        }

        [DataMember]
        public bool Skippable
        {
            get { return skippable; }
            set
            {
                skippable = value;
                RaisePropertyChanged("Skippable");
            }
        }

        [DataMember]
        public string ID
        {
            get { return id; }
            set
            {
                id = value;
                RaisePropertyChanged("ID");
            }
        }

        public override LuaTable GetLuaTable(Mission mission)
        {
            var map = new Dictionary<object, object>
				{
					{"instant", Instant},
                    {"id", ID},
                    {"skippable", Skippable}
				};
            return new LuaTable(map);
        }

		public override string GetDefaultName()
		{
			return "Enter Cutscene";
		}
	}
}