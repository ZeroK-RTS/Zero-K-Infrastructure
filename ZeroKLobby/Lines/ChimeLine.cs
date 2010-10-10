using System;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    class ChimeLine : IChatLine
    {
        public ChimeLine()
        {
            Text = TextColor.History + "*** It is now " + DateTime.Now.ToShortTimeString() + ".";
        }

        public string Text { get; private set; }
    }
}