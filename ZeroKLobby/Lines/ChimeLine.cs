using System;
using SpringDownloader.MicroLobby;

namespace SpringDownloader.Lines
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