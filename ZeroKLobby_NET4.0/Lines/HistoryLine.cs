using System;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    class HistoryLine: IChatLine
    {
        public HistoryLine(string text)
        {
			Date = DateTime.Now;
            Text = TextColor.History + text.StripAllCodes();
        }

        public string Text { get; private set; }
		public DateTime Date { get; set; }
    }
}