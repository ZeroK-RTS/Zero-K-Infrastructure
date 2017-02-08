using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;

namespace ZkLobbyServer
{
    public interface IChatRelaySource
    {
        List<string> GetUsers(string channel);

        event Action<IChatRelaySource, ChatRelayMessage> OnChatRelayMessage;
        void SendMessage(ChatRelayMessage message);

        void SendPm(string user, string message);
    }

    public class ChatRelayMessage
    {
        public string Channel;
        public string User;
        public string Message;
        public SaySource? Source;
        public bool IsEmote;

        public ChatRelayMessage(string channel, string user, string message, SaySource? source, bool isEmote)
        {
            Channel = channel;
            User = user;
            Message = message;
            Source = source;
            IsEmote = isEmote;
        }

        public Say ToSay()
        {
            return new Say()
            {
                AllowRelay = false,
                IsEmote = IsEmote,
                Place = SayPlace.Channel,
                Source = Source != SaySource.Zk ? Source : null, // do not send "zk" source to save traffic 
                Target = Channel,
                User = User,
                Text = Message
            };
        }
    }
}