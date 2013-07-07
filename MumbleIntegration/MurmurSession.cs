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

        [Flags]
        public enum Permission
        {
            Ban = 0x20000,
            Enter = 0x04,
            Kick = 0x10000,
            LinkChannel = 0x80,
            MakeChannel = 0x40,
            MakeTempChannel = 0x400,
            Move = 0x20,
            MuteDeafen = 0x10,
            Register = 0x40000,
            Self = 0x80000,
            Speak = 0x08,
            TextMessage = 0x200,
            Traverse = 0x02,
            Whisper = 0x100,
            Write = 0x01
        }

        readonly ServerPrx server;
        readonly Dictionary<int, User>.ValueCollection users;


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


        /// <summary>
        /// Links channels together so that destination can hear source
        /// </summary>
        public void LinkChannel(int sourceID, int destinationID, bool oneWay = true) {
            var channels = server.getChannels();

            // link channels
            var srcChan = channels[sourceID];
            var dstChan = channels[destinationID];
            srcChan.links = new[] { destinationID };
            dstChan.links = new[] { sourceID };
            server.setChannelState(srcChan);
            server.setChannelState(dstChan);

            if (oneWay) {
                // set ACL (dont let source hear destination)
                server.setACL(sourceID,
                              new[]
                              {
                                  new ACL(true, true, false, -1, "all", 0, (int)Permission.Speak),
                                  new ACL(true, true, false, -1, "in", (int)Permission.Speak, 0)
                              },
                              null,
                              true);
            }
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