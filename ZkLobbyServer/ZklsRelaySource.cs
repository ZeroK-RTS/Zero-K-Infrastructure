using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer
{
    public class ZklsRelaySource: IChatRelaySource
    {
        private ZkLobbyServer server;

        public ZklsRelaySource(ZkLobbyServer server)
        {
            this.server = server;
            server.Said += Said;
        }

        private void Said(object sender, Say say)
        {
            try
            {
                if (say.AllowRelay && say.Place == SayPlace.Channel)
                {
                    OnChatRelayMessage?.Invoke(this, new ChatRelayMessage(say.Target, say.User, say.Text, SaySource.Zk, false));
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error relaying from ZKLS: {0}",ex);
            }
        }

        public List<string> GetUsers(string channel)
        {
            return server.Channels.Get(channel)?.Users?.Keys?.ToList() ?? new List<string>();
        }

        public event Action<IChatRelaySource, ChatRelayMessage> OnChatRelayMessage;
        public void SendMessage(ChatRelayMessage message)
        {
            try
            {
                if (message.Source != SaySource.Zk)
                {
                    server.GhostSay(message.ToSay());
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error relaying message to Zkls: {0}", ex); 
            }
        }

        public void SendPm(string user, string message)
        {
            try
            {
                server.GhostPm(user, message);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error sending zkls pm: {0}",ex);
            }
        }
    }
}
