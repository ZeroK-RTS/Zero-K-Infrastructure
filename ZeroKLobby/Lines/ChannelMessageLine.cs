using System;
using SpringDownloader.MicroLobby;

namespace SpringDownloader.Lines
{
    public class ChannelMessageLine: IChatLine
    {
        public DateTime Date { get; set; }
        public string Message { get; set; }

        public ChannelMessageLine(string message)
        {
            Message = message;
            Date = DateTime.Now;
            Text = TextColor.Text + "[" + TextColor.Date + Date.ToShortTimeString() + TextColor.Text + "] " + TextColor.Message + "Channel message: " +
                   Message;
        }

        public string Text { get; private set; }
    }
}