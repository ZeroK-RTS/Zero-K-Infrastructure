using System;
using System.Runtime.Serialization;
using System.Windows.Media;
using CMissionLib.UnitSyncLib;

namespace CMissionLib
{
	[DataContract]
	public class Player: PropertyChanged
	{
		string aiDll = "";
		string aiVersion;
		string alliance = "Alliance 1";
		Color color = Colors.Red;
		bool isHuman = true;
		bool isRequired;
		string name = "New Player";
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
		public string AIDll
		{
			get { return String.IsNullOrEmpty(aiDll) ? "NullAI" : aiDll; }
			set
			{
				aiDll = value;
				RaisePropertyChanged("AIDll");
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
		public bool IsHuman
		{
			get { return isHuman; }
			set
			{
				isHuman = value;
				if (!value) if (name != null) name = name.Replace(" ", "_");
				RaisePropertyChanged("IsHuman");
			}
		}

		[DataMember]
		public bool IsRequired
		{
			get { return isRequired; }
			set
			{
				isRequired = value;
				RaisePropertyChanged("IsRequired");
			}
		}


		[DataMember]
		public string Name
		{
			get { return name; }
			set
			{
				name = IsHuman ? value : value.Replace(" ", "_");
				RaisePropertyChanged("Name");
			}
		}


		public Ai ai;

		public override string ToString()
		{
			return name;
		}
	}
}