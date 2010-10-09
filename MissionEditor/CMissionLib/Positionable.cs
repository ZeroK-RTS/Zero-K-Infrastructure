using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;

namespace CMissionLib
{
	[DataContract]
	public abstract class Positionable : PropertyChanged
	{
		double x;
		double y;

		public Positionable(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		[DataMember]
		public double X
		{
			get { return x; }
			set 
			{ 
				x = value;
				RaisePropertyChanged("Point");
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
				RaisePropertyChanged("Point");
				RaisePropertyChanged("Y");
			}
		}

		[DataMember]
		public Point Point
		{
			get
			{
				return new Point(x, y);
			}
			set { x = value.X;
				y = value.Y;
				RaisePropertyChanged("Point");
				RaisePropertyChanged("X");
				RaisePropertyChanged("Y");
			}
		}
	}
}
