using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public ChatRelay(ZkLobbyServer zkServer, List<string> channels)
        {
            this.channels = channels;
            this.server = zkServer;

            sources.Add(new SpringRelaySource(channels));
            sources.Add(new DiscordRelaySource());
            sources.Add(new ZklsRelaySource(zkServer));

            foreach (var s in sources) s.OnChatRelayMessage += OnAnySourceMessage;
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
                            .Select(x => $"{x.Mode} {x.NonSpectatorCount}+{x.SpectatorCount}/{x.MaxPlayers} {x.MapName} {x.Title}")));

                    source.SendPm(msg.User, sb.ToString());
                }
                else
                {
                    foreach (var s in sources.Where(x => x != source)) s.SendMessage(msg);
                }
        }
    }
}