using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;

namespace MumbleIntegration
{
    public class MumbleMover
    {
        readonly TasClient client;

        public MumbleMover(TasClient client) {
            this.client = client;

            client.BattleUserJoined += (sender, args) =>
                {
                    var bat = client.ExistingBattles[args.BattleID];
                    if (bat.Founder.IsSpringieManaged && !client.ExistingUsers[args.UserName].IsInGame) {
                        var murmur = new MurmurSession();
                        murmur.MoveUser(args.UserName, murmur.GetOrCreateChannelID(MurmurSession.ZkRootNode, bat.Founder.Name, "Spectators"));
                    }
                };

            client.BattleEnded += (sender, args) =>
                {
                    if (args.Data.Founder.IsSpringieManaged && !args.Data.Founder.IsInGame) {
                        var murmur = new MurmurSession();
                        var specChan = murmur.GetOrCreateChannelID(MurmurSession.ZkRootNode, args.Data.Founder.Name, "Spectators");
                        foreach (var us in args.Data.Users) murmur.MoveUser(us.Name, specChan);
                    }
                };
        }

        public void OnBalance(string autohostName, bool isGameStart, List<PlayerInfo> players) {
            try {
                if (client.ExistingUsers[autohostName].IsInGame) return;// ignoring balance when in game

                var murmur = new MurmurSession();
                var specchan = murmur.GetOrCreateChannelID(MurmurSession.ZkRootNode, autohostName, "Spectators");
                if (!isGameStart) foreach (var p in players) murmur.MoveUser(p.Name, specchan);
                else {
                    foreach (var p in players.Where(x => x.IsSpectator)) murmur.MoveUser(p.Name, specchan);

                    foreach (var allyGrp in players.Where(x => !x.IsSpectator).GroupBy(x => x.AllyID)) {
                        var chan = murmur.GetOrCreateChannelID(MurmurSession.ZkRootNode, autohostName, "Team" + (allyGrp.Key + 1));
                        murmur.LinkChannel(chan, specchan);
                        foreach (var p in allyGrp) murmur.MoveUser(p.Name, chan);
                    }
                }
            } catch (Exception ex) {
                try {
                    client.Say(TasClient.SayPlace.User, "Licho", ex.ToString(), false); // todo remove when tested
                } catch {}
            }
        }

        public class PlayerInfo
        {
            public int AllyID;
            public bool IsSpectator;
            public string Name;
        }
    }
}