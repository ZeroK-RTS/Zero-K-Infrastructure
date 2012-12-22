using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class FadeInAction : Action
	{
        bool instant = false;

		public FadeInAction()
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
			return "Fade In";
		}
	}
}