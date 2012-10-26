using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ConvoMessageAction : Action
	{
		string imagePath;
        string soundPath;
		string message;
        int fontSize = 14;
        int time = 150; // gameframes

		public ConvoMessageAction(string message)
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
        public string SoundPath
        {
            get { return soundPath; }
            set
            {
                soundPath = value;
                RaisePropertyChanged("SoundPath");
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

        [DataMember]
        public int Time
        {
            get { return time; }
            set
            {
                time = value;
                RaisePropertyChanged("Time");
            }
        }

		public override LuaTable GetLuaTable(Mission mission)
		{
            Dictionary<object, object> map = new Dictionary<object, object>
				{
					{"message", message},
                    {"fontSize", FontSize},
                    {"time", time},
				};
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(ImagePath))
			{
				var image = new BitmapImage(new Uri(ImagePath));
				map.Add("image", Path.GetFileName(ImagePath));
				map.Add("imageWidth", image.PixelWidth);
                map.Add("imageHeight", image.PixelHeight);
			}
            if (!string.IsNullOrEmpty(soundPath) && File.Exists(SoundPath))
            {
                map.Add("sound", Path.GetFileName(soundPath));
            }

            return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Convo Message";
		}
	}
}