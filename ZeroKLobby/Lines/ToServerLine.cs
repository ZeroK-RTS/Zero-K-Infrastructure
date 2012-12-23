using System;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
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
            if (args != null) Args = String.Join(" ", args);

            Text = string.Format("{0}[{1}{2}{0}] {3}{4}{5} {6}", TextColor.Text, TextColor.Date, Date.ToShortTimeString(), TextColor.OutgoingCommand, Command, TextColor.Args, Args);
        }

        public string Text { get; private set; }
    }
}