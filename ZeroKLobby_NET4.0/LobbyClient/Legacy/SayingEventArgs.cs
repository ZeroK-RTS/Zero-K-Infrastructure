﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LobbyClient.Legacy
{
    [Obsolete]
    public class SayingEventArgs : EventArgs
	{
	    public SayingEventArgs(TasClient.SayPlace sayPlace, string channel, string text, bool isEmote)
	    {
	        SayPlace = sayPlace;
	        Channel = channel;
	        Text = text;
	        IsEmote = isEmote;
	    }

	    public bool Cancel { get; set; }
        public TasClient.SayPlace SayPlace { get; set; }
        public string Channel { get; set; }
        public string Text { get; set; }
        public bool IsEmote { get; set; }
	}
}
