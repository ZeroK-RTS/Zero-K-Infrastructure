using System;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    class ChimeLine : IChatLine
    {
    	public DateTime Date { get; set; }

        public ChimeLine()
        {
			Date = DateTime.Now;
            Text = TextColor.History + "*** It is now " + DateTime.Now.ToShortTimeString() + ".";
        }

        public string Text { get; private set; }
    }
}