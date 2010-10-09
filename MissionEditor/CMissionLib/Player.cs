using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Media;

namespace CMissionLib
{
	[DataContract]
	public class Player : PropertyChanged
	{
		string name = "New Player";
		Color color = Colors.Red;
		string alliance = "Alliance 1";
		bool isHuman = true;
		string aiDll = "";

		[DataMember]
		public string Name
		{
			get { return name; }
			set
			{
				name = value;
				RaisePropertyChanged("Name");
			}
		}

		[DataMember]
		public Color Color
		{
			get { return color; }
			set
			{
				color = value;
				RaisePropertyChanged("Color");
				RaisePropertyChanged("ColorBrush");
			}
		}


		[DataMember]
		public SolidColorBrush ColorBrush
		{
			get
			{
				var brush = new SolidColorBrush(color);
				brush.Freeze();
				return brush;
			}
			set
			{
				var v = (SolidColorBrush)value; // this is just to make data binding happy anyway, should never happen
				color = v.Color;
				RaisePropertyChanged("Color");
				RaisePropertyChanged("ColorBrush");
			}
		}

		[DataMember]
		public string Alliance
		{
			get { return alliance; }
			set
			{
				alliance = value;
				RaisePropertyChanged("Alliance");
			}
		}


		[DataMember]
		public bool IsHuman
		{
			get { return isHuman; }
			set
			{
				isHuman = value;
				RaisePropertyChanged("IsHuman");
			}
		}

		[DataMember]
		public string AIDll
		{
			get { return aiDll; }
			set
			{
				aiDll = value;
				RaisePropertyChanged("AIDll");
			}
		}

		public override string ToString()
		{
			return name;
		}


	}
}
