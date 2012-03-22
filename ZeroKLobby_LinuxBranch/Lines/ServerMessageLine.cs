using System;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    public class ServerMessageLine: IChatLine
    {
        public DateTime Date { get; set; }
        public string Message { get; set; }

        public ServerMessageLine(string message)
        {
            Message = message;
            Date = DateTime.Now;
            Text = TextColor.Text + "[" + TextColor.Date + Date.ToShortTimeString() + TextColor.Text + "] " + TextColor.Message + "Server message: " +
                   Message;
        }

        public string Text { get; private set; }
    }
}