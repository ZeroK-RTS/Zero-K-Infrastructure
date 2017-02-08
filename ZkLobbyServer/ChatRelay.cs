using System.Collections.Generic;
using System.Linq;

namespace ZkLobbyServer
{
    /// <summary>
    ///     Relays chat between sources
    /// </summary>
    public class ChatRelay
    {
        private readonly List<string> channels;
        private readonly List<IChatRelaySource> sources = new List<IChatRelaySource>();

        public ChatRelay(ZkLobbyServer zkServer, List<string> channels)
        {
            this.channels = channels;

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

                    source.SendPm(msg.User, string.Join("\n", users));
                }
                else
                {
                    foreach (var s in sources.Where(x => x != source)) s.SendMessage(msg);
                }
        }
    }
}