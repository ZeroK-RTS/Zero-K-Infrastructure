using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib
{
	[DataContract]
	public class RectangularArea : Area
	{
		double x;
		double y;
		double width;
		double height;


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
		public double Height
		{
			get { return height; }
			set
			{
				height = value;
				RaisePropertyChanged("Height");
			}
		}

		[DataMember]
		public double Width
		{
			get { return width; }
			set
			{
				width = value;
				RaisePropertyChanged("Width");
			}
		}

		public override LuaTable GetLuaMap(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"category", "rectangle"},
					{"x", mission.ToIngameX(X)},
					{"y", mission.ToIngameY(Y)},
					{"width", mission.ToIngameX(width)},
					{"height", mission.ToIngameX(height)},
				};
			return new LuaTable(map);
		}
	}
}
