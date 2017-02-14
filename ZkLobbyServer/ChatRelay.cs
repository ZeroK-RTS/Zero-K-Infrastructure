using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Discord;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    /// <summary>
    ///     Relays chat between sources
    /// </summary>
    public class ChatRelay
    {
        private readonly List<string> channels;
        private readonly List<IChatRelaySource> sources = new List<IChatRelaySource>();
        private ZkLobbyServer server;
        private DiscordRelaySource discordZkRelay;
        private DiscordRelaySource discordSpringRelay;
        private Timer timer;
        private string lastZkTopic;
        private SpringRelaySource springRelay;

        private const ulong DiscordZkServerID = 278805140708786177;
        private const ulong DiscordSpringServerID = 223585969956323328;

        public ChatRelay(ZkLobbyServer zkServer, List<string> channels)
        {
            this.channels = channels;
            this.server = zkServer;

            var discord = new DiscordClient();

            springRelay = new SpringRelaySource(channels);
            sources.Add(springRelay);
            sources.Add(new ZklsRelaySource(zkServer));
            discordZkRelay = new DiscordRelaySource(discord, DiscordZkServerID, SaySource.Discord);
            sources.Add(discordZkRelay);
            discordSpringRelay = new DiscordRelaySource(discord, DiscordSpringServerID, SaySource.DiscordSpring);
            sources.Add(discordSpringRelay);

            discord.Connect(new Secrets().GetNightwatchDiscordToken(), TokenType.Bot);

            foreach (var s in sources) s.OnChatRelayMessage += OnAnySourceMessage;

            timer = new Timer(TimerCallback, this, 1000, 2000);
        }

        private void TimerCallback(object state)
        {
            try
            {
                var zkTopic =
                    $"Zero-K game server: {server.ConnectedUsers.Count} online, {server.MatchMaker.GetTotalWaiting()} in queue, {server.Battles.Values.Where(x => x != null).Sum(x => (int?)x.NonSpectatorCount + x.SpectatorCount) ?? 0} in custom games";

                if (zkTopic != lastZkTopic)
                {
                    foreach (var ch in channels) discordZkRelay?.SetTopic(ch, zkTopic);
                }
                lastZkTopic = zkTopic;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing relay timer: {0}",ex);
            }
        }

        private void OnAnySourceMessage(IChatRelaySource source, ChatRelayMessage msg)
        {
            if (channels.Contains(msg.Channel))
                if (msg.Message.StartsWith("!names"))
                {
                    var users = new List<string>();
                    foreach (var s in sources.Where(x => x != source)) users.AddRange(s.GetUsers(msg.Channel));
                    users = users.Distinct().OrderBy(x=>x).ToList();

                    source.SendPm(msg.User, string.Join("\n", users));
                }
                else if (msg.Message.StartsWith("!games"))
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat("MatchMaker queue {0}\n", string.Join(", ", server.MatchMaker.GetQueueCounts().Select(x => $"{x.Key}: {x.Value}")));
                    sb.Append(string.Join("\n",
                        server.Battles.Values.Where(x => x != null)
                            .OrderByDescending(x => x.NonSpectatorCount)
                            .Select(x => $"{x.Mode.Description()} {x.NonSpectatorCount}+{x.SpectatorCount}/{x.MaxPlayers} {x.MapName} {x.Title}")));

                    source.SendPm(msg.User, sb.ToString());
                }
                else
                {
                    foreach (var s in sources.Where(x => x != source)) s.SendMessage(msg);
                }
        }
    }
}