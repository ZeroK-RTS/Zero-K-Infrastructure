using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	/// <summary>
	/// Set whether the selected unit group is always visible
	/// </summary>
	[DataContract]
	public class MakeUnitsAlwaysVisibleAction : Action
	{
        bool value = true;
		string group = String.Empty;

		public MakeUnitsAlwaysVisibleAction()
			: base() {}

		[DataMember]
		public string Group
		{
			get { return group; }
			set
			{
				group = value;
				RaisePropertyChanged("Group");
			}
		}

        [DataMember]
        public bool Value
        {
            get { return value; }
            set
            {
                this.value = value;
                RaisePropertyChanged("Value");
            }
        }

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"group", group},
                    {"value", value},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Make Units Always Visible";
		}
	}
}