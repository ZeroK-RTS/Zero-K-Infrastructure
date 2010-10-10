using System;
using SpringDownloader.MicroLobby;

namespace SpringDownloader.Lines
{
    public class BroadcastLine: IChatLine
    {
        public DateTime Date { get; set; }
        public string Message { get; set; }

        public BroadcastLine(string message)
        {
            Message = message;
            Date = DateTime.Now;
            Text = TextColor.Text + "[" + TextColor.Date + Date.ToShortTimeString() + TextColor.Text + "] " + TextColor.Message + "Broadcast: " +
                   Message;
        }

        public string Text { get; private set; }
    }
}