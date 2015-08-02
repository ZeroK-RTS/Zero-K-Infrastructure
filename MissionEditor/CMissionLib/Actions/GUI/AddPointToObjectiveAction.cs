using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class AddPointToObjectiveAction : Action
	{
        string id;
		string group = String.Empty;

        public AddPointToObjectiveAction(double x, double y)
            : base()
		{
            this.Y = y;
            this.X = x;
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

        [DataMember]
        public double Y { get; set; }

        [DataMember]
        public double X { get; set; }

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"id", ID},
                    {"x", mission.ToIngameX(X)},
					{"y", mission.ToIngameY(Y)},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Add Point to Objective";
		}
	}
}