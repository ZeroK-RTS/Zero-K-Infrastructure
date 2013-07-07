using System;
using System.Collections.Generic;
using System.Linq;
using Ice;
using Murmur;

namespace MumbleIntegration
{
    public class MurmurSession
    {
        public const string ZkRootNode = " Zero-K";
        readonly ServerPrx server;
        Dictionary<int, User>.ValueCollection users;


        public MurmurSession() {
            var communicator = Util.initialize();
            var proxy = communicator.stringToProxy("Meta:tcp -h 94.23.170.70 -p 6502");
            var meta = MetaPrxHelper.uncheckedCast(proxy);
            server = meta.getAllServers().First();
            users = server.getUsers().Values;
        }

        public int GetOrCreateChannelID(params string[] path) {
            var parentId = 0;
            var channels = server.getChannels().Values;

            foreach (var node in path) {
                var entry =
                    channels.FirstOrDefault(x => x.parent == parentId && string.Equals(x.name, node, StringComparison.InvariantCultureIgnoreCase));
                if (entry != null) parentId = entry.id;
                else parentId = server.addChannel(node, parentId);
            }
            return parentId;
        }

        public void MoveUser(string name, int channel) {
            var user = users.FirstOrDefault(x => string.Equals(x.name, name, StringComparison.InvariantCultureIgnoreCase));
            if (user != null) {
                user.channel = channel;
                server.setState(user);
            }
        }

    }
}