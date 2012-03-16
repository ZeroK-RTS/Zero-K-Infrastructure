using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class CustomAction2 : Action
	{
        string codeStr;

        public CustomAction2()
		{
            CodeStr = "";
		}

        [DataMember]
        public string CodeStr
        {
            get { return codeStr; }
            set
            {
                codeStr = value;
                RaisePropertyChanged("CodeStr");
            }
        }

		public override LuaTable GetLuaTable(Mission mission)
		{
            var map = new Dictionary<object, object>
				{
					{"codeStr", CodeStr},
				};
            return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Custom Action (Alternate)";
		}
	}
}