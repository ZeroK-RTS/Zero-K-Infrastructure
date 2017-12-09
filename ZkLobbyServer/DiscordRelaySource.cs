using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class DiscordRelaySource : IChatRelaySource
    {
        private DiscordSocketClient discord;
        private ulong serverID;
        private SaySource source;

        private static string GetName(IUser user)
        {
            return user.Username + "#" + user.Discriminator;
        }

        public DiscordRelaySource(DiscordSocketClient client, ulong serverID, SaySource source)
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
                GetChannel(channel)?.ModifyAsync(prop => { prop.Topic = topic; });
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
                    if (m.User != GlobalConst.NightwatchName) GetChannel(m.Channel)?.SendMessageAsync($"<{m.User}> {m.Message}");
                    // don't relay extra "nightwatch" if it is self relay
                    else GetChannel(m.Channel)?.SendMessageAsync(m.Message);
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
                discord.GetGuild(serverID).Users.FirstOrDefault(x => GetName(x) == user)?.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error sending discord pm:{0}",ex);
            }
        }


        private async Task DiscordOnMessageReceived(SocketMessage msg)
        {
            try
            {
                if (discord.GetGuild(serverID).GetChannel(msg.Channel.Id) != null) if (!msg.Author.IsBot && msg.Author.Username != GlobalConst.NightwatchName) OnChatRelayMessage?.Invoke(this, new ChatRelayMessage(msg.Channel.Name, GetName(msg.Author), msg.Content, source, false));
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error receiving discord message:{0}",ex);
            }
        }

        private SocketTextChannel GetChannel(string name)
        {
            return discord?.GetGuild(serverID)?.TextChannels.FirstOrDefault(x => x.Name == name);
        }
    }
}