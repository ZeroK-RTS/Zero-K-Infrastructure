using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using PlasmaShared;

namespace ZkLobbyServer
{
    public partial class MatchMaker
    {
        public class PlayerEntry
        {
            public bool InvitedToPlay;
            public bool LastReadyResponse;

            public int EloWidth => (int)(100.0 + WaitRatio * 300.0);
            public int MinConsideredElo => LobbyUser.EffectiveMmElo;
            public int MaxConsideredElo => (int)(LobbyUser.EffectiveMmElo + (LobbyUser.RawMmElo - LobbyUser.EffectiveMmElo) * WaitRatio);

            public double WaitRatio => Math.Max(0, Math.Min(1.0, DateTime.UtcNow.Subtract(JoinedTime).TotalSeconds / 60.0));

            public DateTime JoinedTime { get; private set; } = DateTime.UtcNow;
            public User LobbyUser { get; private set; }
            public string Name => LobbyUser.Name;
            public List<MatchMakerSetup.Queue> QueueTypes { get; private set; }
            public PartyManager.Party Party { get; set; }


            public PlayerEntry(User user, List<MatchMakerSetup.Queue> queueTypes, PartyManager.Party party)
            {
                Party = party;
                QueueTypes = queueTypes;
                LobbyUser = user;
            }

            public List<ProposedBattle> GenerateWantedBattles(List<PlayerEntry> allPlayers)
            {
                var ret = new List<ProposedBattle>();
                foreach (var qt in QueueTypes)
                {
                    // variable game size, allow smaller games the longer the wait of longest waiting player
                    var qtMaxWait = qt.MaxSize > qt.MinSize ? allPlayers.Where(x => x.QueueTypes.Contains(qt)).Max(x => x.WaitRatio) : 0; 

                    for (var i = qt.MaxSize; i >= qt.MaxSize - (qt.MaxSize - qt.MinSize) * qtMaxWait; i--)
                        if (qt.Mode == AutohostMode.GameChickens || i % 2 == 0)
                        {
                            if (Party == null || (qt.Mode == AutohostMode.GameChickens && Party.UserNames.Count<=i) || Party.UserNames.Count <= i / 2) ret.Add(new ProposedBattle(i, this, qt, qt.EloCutOffExponent, allPlayers));
                        }
                }
                return ret;
            }

            public void UpdateTypes(List<MatchMakerSetup.Queue> queueTypes)
            {
                QueueTypes = queueTypes;
            }
        }
    }
}