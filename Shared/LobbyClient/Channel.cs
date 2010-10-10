using System;
using System.Collections.Generic;

namespace LobbyClient
{
    /// <summary>
    /// Channel - active joined channels
    /// </summary>
    public class Channel
    {
        public List<string> ChannelUsers { get; set; }
        public string Name { get; set; }
        public string Topic { get; set; }
        public string TopicSetBy { get; set; }
        public DateTime TopicSetDate { get; set; }

        public static Channel Create(string name)
        {
            var c = new Channel();
            c.Name = name;
            c.ChannelUsers = new List<string>();
            return c;
        }
    } ;
}