using System;
using SpringDownloader.MicroLobby;

namespace SpringDownloader.Lines
{
    public class ToServerLine: IChatLine
    {
        public string Args { get; set; }
        public string Command { get; set; }
        public DateTime Date { get; set; }

        public ToServerLine(string command, string[] args)
        {
            Command = command;
            Date = DateTime.Now;
            Args = String.Join(" ", args);

            Text = TextColor.Text + "[" + TextColor.Date + Date.ToShortTimeString() + TextColor.Text + "] " + TextColor.OutgoingCommand + Command +
                   TextColor.Args + " " + Args;
        }

        public string Text { get; private set; }
    }
}