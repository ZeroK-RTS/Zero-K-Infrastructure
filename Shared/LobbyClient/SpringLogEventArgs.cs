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

    public class SpringChatEventArgs : SpringLogEventArgs
    {
        public SpringChatLocation Location { get; }

        public SpringChatEventArgs(string username, string line, SpringChatLocation location) : base(username, line)
        {
            this.Location = location;
        }
    };

    public enum SpringChatLocation
    {
        Public, Allies, Spectators,
        Private
    }
}