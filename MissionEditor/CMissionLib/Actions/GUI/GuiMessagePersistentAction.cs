using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;

namespace CMissionLib.Actions
{
	[DataContract]
	public class GuiMessagePersistentAction : Action
	{
		string imagePath;
		string message;
		int width = 320;
        int height = 100;
        int fontSize = 12;

		public GuiMessagePersistentAction(string message)
		{
			this.message = message;
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

        [DataMember]
        public int Height
        {
            get { return height; }
            set
            {
                height = value;
                RaisePropertyChanged("Height");
            }
        }

        [DataMember]
        public int FontSize
        {
            get { return fontSize; }
            set
            {
                fontSize = value;
                RaisePropertyChanged("FontSize");
            }
        }
		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"message", message},
					{"width", Width},
                    {"height", Height},
                    {"fontSize", FontSize},
				};
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(ImagePath))
            {
               map.Add("image", Path.GetFileName(ImagePath));
            }
            else if (!string.IsNullOrWhiteSpace(imagePath))
            {
                map.Add("image", imagePath);
                map.Add("imageFromArchive", true);
            }
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "GUI Message (Persistent)";
		}
	}
}