using System;
using Newtonsoft.Json;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    public class FromServerLine: IChatLine
    {
        public string Args { get; set; }
        public string Command { get; set; }
        public DateTime Date { get; set; }

        public FromServerLine(object o)
        {
            Command = o.GetType().Name;
            Date = DateTime.Now;
            Args = JsonConvert.SerializeObject(o);
            Text = string.Format("{0}[{1}{2}{0}] {3}{4}{5} {6}", TextColor.Text, TextColor.Date, Date.ToShortTimeString(), TextColor.IncomingCommand, Command, TextColor.Args, Args);
        }

        public string Text { get; private set; }
    }
}