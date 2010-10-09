using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public class Cylinder : Area
	{

		double r;
		double x;
		double y;

		[DataMember]
		public double X
		{
			get { return x; }
			set
			{
				x = value;
				RaisePropertyChanged("X");
			}
		}

		[DataMember]
		public double Y
		{
			get { return y; }
			set
			{
				y = value;
				RaisePropertyChanged("Y");
			}
		}

		[DataMember]
		public double R
		{
			get { return r; }
			set
			{
				r = value;
				RaisePropertyChanged("R");
			}
		}

		public override LuaTable GetLuaMap(Mission mission)
		{
			var map = new Dictionary<string, object>
				{
					{"category", "cylinder"},
					{"x", mission.ToIngameX(X)},
					{"y", mission.ToIngameY(Y)},
					{"r", mission.ToIngameX(R)},
				};
			return new LuaTable(map);
		}
	}
}