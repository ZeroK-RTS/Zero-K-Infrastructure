using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.Rpc;
using Discord.WebSocket;
using LobbyClient;
using PlasmaShared;
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

        
        private readonly List<string> restrictedChannels = new List<string>() { GlobalConst.ModeratorChannel, GlobalConst.CoreChannel }; // restricted channels can only be relayed between restricted sources
        private readonly List<IChatRelaySource> restrictedSources = new List<IChatRelaySource>();

        private ZkLobbyServer server;
        private DiscordRelaySource discordZkRelay;
        private DiscordRelaySource discordSpringRelay;
        private Timer timer;
        private string lastZkTopic;
        private ZklsRelaySource zklsRelay;
        private DiscordSocketClient discord;

        private const ulong DiscordZkServerID = 278805140708786177;
        private const ulong DiscordSpringServerID = 223585969956323328;

        public ChatRelay(ZkLobbyServer zkServer, List<string> channels)
        {
            this.channels = channels;
            this.server = zkServer;

            discord = new DiscordSocketClient();
            

            zklsRelay = new ZklsRelaySource(zkServer);
            sources.Add(zklsRelay);
            
            discordZkRelay = new DiscordRelaySource(discord, DiscordZkServerID, SaySource.Discord);
            sources.Add(discordZkRelay);
            discordSpringRelay = new DiscordRelaySource(discord, DiscordSpringServerID, SaySource.DiscordSpring);
            sources.Add(discordSpringRelay);


            restrictedSources.Add(zklsRelay);
            restrictedSources.Add(discordZkRelay);

            var token = new Secrets().GetNightwatchDiscordToken(); 
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    await discord.StartAsync();
                    await discord.LoginAsync(TokenType.Bot, token);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error initializing discord connection/relay: {0}",ex);
                }
            }).Wait();
            

            foreach (var s in sources) s.OnChatRelayMessage += OnAnySourceMessage;

            timer = new Timer(TimerCallback, this, 1000, 2000);
        }

        public int DiscordZkUserCount { get; private set; }

        private object timerLock = new object();
       private void TimerCallback(object state)
        {
            lock (timerLock)
            {
                try
                {
                    if (discord?.LoginState != LoginState.LoggedIn) return;
                    if (server?.ConnectedUsers?.Count == null) return;
                    DiscordZkUserCount = discord?.GetGuild(DiscordZkServerID)?.Users?.Count ?? 0;
                    var zkTopic =
                        $"[game: {server.ConnectedUsers.Count} online, {server.MatchMaker.GetTotalWaiting()} in queue, {server.Battles.Values.Where(x => x != null).Sum(x => (int?)x.NonSpectatorCount + x.SpectatorCount) ?? 0} in custom]";

                    if (zkTopic != lastZkTopic)
                    {
                        foreach (var ch in channels) discordZkRelay?.SetTopic(ch, $"{server.Channels.Get(ch)?.Topic?.Text} {zkTopic}");
                    }
                    lastZkTopic = zkTopic;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error processing relay timer: {0}", ex);
                }
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
                } else if (restrictedChannels.Contains(msg.Channel))
                {
                    foreach (var s in restrictedSources.Where(x => x != source)) s.SendMessage(msg);
                }
                else
                {
                    foreach (var s in sources.Where(x => x != source)) s.SendMessage(msg);
                }
        }
    }
}