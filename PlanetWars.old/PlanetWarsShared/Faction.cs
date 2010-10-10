#region using

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Serialization;
using PlanetWarsShared.Springie;
using System.Diagnostics;

#endregion

namespace PlanetWarsShared
{
    [Serializable]
	[DebuggerDisplay("{FactionName}")]
	public class Faction : IFaction
	{
        [Serializable]
        public class ChatEvent
        {
            public string Name;
            public string Text;
            public DateTime Time;
            public ChatEvent() { }
            public ChatEvent(string name, string text, DateTime time)
            {
                Name = name;
                Text = text;
                Time = time;
            }
        }

        public int ColorR { get; set; }
		public int ColorG { get; set; }
		public int ColorB { get; set; }

   
        

        public List<ChatEvent> ChatEvents = new List<ChatEvent>();

		[XmlIgnore]
		public Color Color
		{
			get { return Color.FromArgb(ColorR, ColorG, ColorB); }
			set
			{
				ColorR = value.R;
				ColorG = value.G;
				ColorB = value.B;
			}
		}

		#region Constructors

		public Faction(string name, Color color)
		{
		    Name = name;
			Color = color;
			SpringSide = name;
		}

		public Faction()
		{
		}

	    #endregion

		#region IFaction Members

		public string SpringSide { get; set; }
		public string Name { get; set; }

		#endregion

		public override string ToString()
		{
			return ToHtml(Name);
		}

		public static string ToHtml(string name)
		{
			return string.Format("<a href='faction.aspx?name={0}'>{0}</a>", name);
		}
	}
}