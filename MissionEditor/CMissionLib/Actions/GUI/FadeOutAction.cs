using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class FadeOutAction : Action
	{
        bool instant = false;

		public FadeOutAction()
			: base() {}

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

        public override LuaTable GetLuaTable(Mission mission)
        {
            var map = new Dictionary<object, object>
				{
					{"instant", Instant},
				};
            return new LuaTable(map);
        }

		public override string GetDefaultName()
		{
			return "Fade Out";
		}
	}
}