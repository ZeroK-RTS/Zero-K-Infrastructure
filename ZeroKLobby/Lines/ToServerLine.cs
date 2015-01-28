using System;
using Newtonsoft.Json;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    public class ToServerLine: IChatLine
    {
        public string Args { get; set; }
        public string Command { get; set; }
        public DateTime Date { get; set; }

        public ToServerLine(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                var parts = line.Split(new[] { ' ' }, 2);
                if (parts.Length == 2)
                {
                    Command = parts[0];
                    Args = parts[1];
                }
                else Command = line;
            }
            Date = DateTime.Now;

            Text = string.Format("{0}[{1}{2}{0}] {3}{4}{5} {6}", TextColor.Text, TextColor.Date, Date.ToShortTimeString(), TextColor.OutgoingCommand, Command, TextColor.Args, Args);
        }

        public string Text { get; private set; }
    }
}