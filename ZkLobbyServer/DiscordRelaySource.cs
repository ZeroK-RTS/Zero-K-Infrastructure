using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Discord;
using LobbyClient;
using ZkData;
using Channel = Discord.Channel;

namespace ZkLobbyServer
{
    public class DiscordRelaySource : IChatRelaySource
    {
        private DiscordClient discord;
        private ulong serverID;
        private SaySource source;

        private static string GetName(Discord.User user)
        {
            return (user.Nickname ?? user.Name) + "#" + user.Discriminator;
        }

        public DiscordRelaySource(DiscordClient client, ulong serverID, SaySource source)
        {
            discord = client;
            this.source = source;
            discord.MessageReceived += DiscordOnMessageReceived;

            this.serverID = serverID;

        }


        public List<string> GetUsers(string channel)
        {
            return GetChannel(channel)?.Users.Select(x => GetName(x)).ToList();
        }

        public event Action<IChatRelaySource, ChatRelayMessage> OnChatRelayMessage;

        public void SetTopic(string channel, string topic)
        {
            try
            {
                GetChannel(channel)?.Edit(topic: topic);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error setting discord topic: {0}",ex);
            }
        }


        public void SendMessage(ChatRelayMessage m)
        {
            try
            {
                if (m.Source != source)
                {
                    if (m.User != GlobalConst.NightwatchName) GetChannel(m.Channel)?.SendMessage($"<{m.User}> {m.Message}");
                    // don't relay extra "nightwatch" if it is self relay
                    else GetChannel(m.Channel)?.SendMessage(m.Message);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error sending discord message: {0}",ex);
            }
        }

        public void SendPm(string user, string message)
        {
            try
            {
                discord.GetServer(serverID).Users.FirstOrDefault(x => GetName(x) == user)?.SendMessage(message);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error sending discord pm:{0}",ex);
            }
        }


        private void DiscordOnMessageReceived(object sender, MessageEventArgs msg)
        {
            try
            {
                if (msg.Server.Id == serverID) if (!msg.User.IsBot) OnChatRelayMessage?.Invoke(this, new ChatRelayMessage(msg.Channel.Name, GetName(msg.User), msg.Message.Text, source, false));
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error receiving discord message:{0}",ex);
            }
        }

        private Channel GetChannel(string name)
        {
            return discord?.GetServer(serverID)?.AllChannels.FirstOrDefault(x => x.Name == name);
        }
    }
}