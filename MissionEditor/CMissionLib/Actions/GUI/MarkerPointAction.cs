using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class MarkerPointAction : Action, ILocalizable
	{
        string stringID;

		public MarkerPointAction(double x, double y)
			: base()
		{
			Text = String.Empty;
			this.Y = y;
			this.X = x;
		}

		[DataMember]
		public double Y { get; set; }

		[DataMember]
		public double X { get; set; }

        [DataMember]
        public string StringID
        {
            get { return stringID; }
            set
            {
                stringID = value;
                RaisePropertyChanged("StringID");
            }
        }

        [DataMember]
		public string Text { get; set; }

		[DataMember]
		public bool CenterCamera { get; set; }

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
                    {"stringID", stringID},
					{"x", mission.ToIngameX(X)},
					{"y", mission.ToIngameY(Y)},
					{"text", Text},
					{"centerCamera", CenterCamera}
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Marker";
		}
	}
}