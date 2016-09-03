using System;
using System.Collections.Generic;
using System.Linq;

namespace LobbyClient
{
    public class SpringLogEventArgs: EventArgs
    {
        public string Line { get; }

        public string Username { get; }

        public SpringLogEventArgs(string username): this(username, "") {}

        public SpringLogEventArgs(string username, string line)
        {
            Line = line;
            Username = username;
        }
    };
}