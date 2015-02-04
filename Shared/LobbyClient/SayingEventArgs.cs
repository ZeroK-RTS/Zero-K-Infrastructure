using System;
using System.Collections.Generic;
using System.Text;

namespace LobbyClient
{
    public class SayingEventArgs : EventArgs
	{
	    public SayingEventArgs(SayPlace sayPlace, string channel, string text, bool isEmote)
	    {
	        SayPlace = sayPlace;
	        Channel = channel;
	        Text = text;
	        IsEmote = isEmote;
	    }

	    public bool Cancel { get; set; }
        public SayPlace SayPlace { get; set; }
        public string Channel { get; set; }
        public string Text { get; set; }
        public bool IsEmote { get; set; }
	}
}
