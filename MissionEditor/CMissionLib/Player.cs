using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Media;
using CMissionLib.UnitSyncLib;

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
		string aiVersion;

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
				color = value.Color;
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

		public Ai ai;


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

		public Ai AI
		{
			get { return ai; }
			set
			{
				ai = value;
				if (ai == null)
				{
					AIVersion = null;
					AIVersion = null;
				}
				else
				{
					AIVersion = ai.Version;
					AIDll = ai.ShortName;
				}
				RaisePropertyChanged("AI");
			}
		}

		[DataMember]
		public string AIVersion
		{
			get { return aiVersion; }
			set
			{
				aiVersion = value;
				RaisePropertyChanged("AIVersion");
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
