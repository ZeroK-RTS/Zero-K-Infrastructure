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
        private const ulong DiscordZkServerID = 278805140708786177;
        private DiscordClient discord;

        public DiscordRelaySource()
        {
            discord = new DiscordClient();
            discord = new DiscordClient();

            discord.MessageReceived += DiscordOnMessageReceived;

            discord.Connect(new Secrets().GetNightwatchDiscordToken(), TokenType.Bot);
        }


        public List<string> GetUsers(string channel)
        {
            return GetChannel(channel)?.Users.Select(x => x.ToString()).ToList();
        }

        public event Action<IChatRelaySource, ChatRelayMessage> OnChatRelayMessage;

        public void SendMessage(ChatRelayMessage m)
        {
            try
            {
                if (m.Source != SaySource.Discord) GetChannel(m.Channel)?.SendMessage($"<{m.User}> {m.Message}");
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
                discord.GetServer(DiscordZkServerID).Users.FirstOrDefault(x => x.ToString() == user)?.SendMessage(message);
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
                if (!msg.User.IsBot) OnChatRelayMessage?.Invoke(this, new ChatRelayMessage(msg.Channel.Name, msg.User.ToString(), msg.Message.Text, SaySource.Discord, false));
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error receiving discord message:{0}",ex);
            }
        }

        private Channel GetChannel(string name)
        {
            return discord.GetServer(DiscordZkServerID).AllChannels.FirstOrDefault(x => x.Name == name);
        }
    }
}