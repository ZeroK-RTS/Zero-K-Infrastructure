using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
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
                    if (m.User != GlobalConst.NightwatchName)
                    {
                        GetChannel(m.Channel)?.SendMessageAsync($"<{m.User}> {m.Message}");
                    }
                    else
                    {
                        // don't relay extra "nightwatch" if it is self relay
                        Trace.TraceInformation($"Relaying NW message {m.Message} to {m.Channel}");
                        GetChannel(m.Channel)?.SendMessageAsync(m.Message);
                    }
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


        private static string TranslateMentions(SocketMessage msg)
        {
            var text = msg.Content;
            if (string.IsNullOrEmpty(text)) return string.Empty;

            return Regex.Replace(text, "<@([0-9]+)>",
                m =>
                {
                    var mentionedId = m.Groups[1].Value;

                    var user = msg.MentionedUsers.FirstOrDefault(x => x.Id.ToString() == mentionedId);
                    if (user != null) return user.Username;

                    var channel = msg.MentionedChannels.FirstOrDefault(x => x.Id.ToString() == mentionedId);
                    if (channel != null) return channel.Name;

                    var role = msg.MentionedRoles.FirstOrDefault(x => x.Id.ToString() == mentionedId);
                    if (role != null) return role.Name;

                    return mentionedId;
                });

        }


        private async Task DiscordOnMessageReceived(SocketMessage msg)
        {
            try
            {
                if (msg?.Channel == null || msg.Author == null || discord == null) return;
                
                if (discord.GetGuild(serverID).GetChannel(msg.Channel.Id) != null) 
                    if (!msg.Author.IsBot && msg.Author.Username != GlobalConst.NightwatchName) 
                        OnChatRelayMessage?.Invoke(this, new ChatRelayMessage(msg.Channel.Name, GetName(msg.Author), TranslateMentions(msg), source, false));

                
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