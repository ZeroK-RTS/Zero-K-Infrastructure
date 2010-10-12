using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;

namespace CMissionLib.Actions
{
	[DataContract]
	public class GuiMessageAction : Action
	{
		string imagePath;
		string message;
		int width = 400;

		public GuiMessageAction(string message)
			: base("GUI Message")
		{
			this.message = message;
			Pause = true;
		}

		[DataMember]
		public string Message
		{
			get { return message; }
			set
			{
				message = value;
				RaisePropertyChanged("Message");
			}
		}

		[DataMember]
		public bool Pause { get; set; }

		[DataMember]
		public string ImagePath
		{
			get { return imagePath; }
			set
			{
				imagePath = value;
				RaisePropertyChanged("ImagePath");
			}
		}

		[DataMember]
		public int Width
		{
			get { return width; }
			set
			{
				width = value;
				RaisePropertyChanged("Width");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			if (string.IsNullOrEmpty(imagePath) || !File.Exists(ImagePath))
			{
				var map = new Dictionary<string, object>
					{
						{"message", message},
						{"width", Width},
						{"pause", Pause},
					};
				return new LuaTable(map);
			}
			else
			{
				var image = new BitmapImage(new Uri(ImagePath));
				var map = new Dictionary<string, object>
					{
						{"message", message},
						{"image", Path.GetFileName(ImagePath)},
						{"imageWidth", image.PixelWidth},
						{"imageHeight", image.PixelHeight},
						{"pause", Pause},
					};
				return new LuaTable(map);
			}
		}
	}
}