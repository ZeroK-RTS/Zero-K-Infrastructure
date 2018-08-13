using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer
{
    public partial class MatchMaker
    {
        public class PlayerEntry
        {
            public bool InvitedToPlay;
            public bool LastReadyResponse;

            public int EloWidth => (int)(DynamicConfig.Instance.MmStartingWidth + WaitRatio * DynamicConfig.Instance.MmWidthGrowth);
            public int MinConsideredElo => LobbyUser.EffectiveMmElo;
            public int MaxConsideredElo => (int)(LobbyUser.EffectiveMmElo + (Math.Max(1500, LobbyUser.RawMmElo) - LobbyUser.EffectiveMmElo) * WaitRatio);

            public double WaitRatio => Math.Max(0, Math.Min(1.0, DateTime.UtcNow.Subtract(JoinedTime).TotalSeconds / DynamicConfig.Instance.MmWidthGrowthTime));
            public double SizeWaitRatio => Math.Max(0, Math.Min(1.0, DateTime.UtcNow.Subtract(JoinedTime).TotalSeconds / DynamicConfig.Instance.MmSizeGrowthTime));

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

            //override elo width growth to find matches instantly
            public void SetMaximumEloWidth()
            {
                JoinedTime = DateTime.UtcNow.AddHours(-1);
            }

            public List<ProposedBattle> GenerateWantedBattles(List<PlayerEntry> allPlayers, bool ignoreSizeLimit)
            {
                var ret = new List<ProposedBattle>();
                foreach (var qt in QueueTypes)
                {
                    // variable game size, allow smaller games the longer the wait of longest waiting player
                    var qtMaxWait = qt.MaxSize > qt.MinSize ? allPlayers.Where(x => x.QueueTypes.Contains(qt)).Max(x => x.SizeWaitRatio) : 0; 

                    for (var i = qt.MaxSize; i >= (ignoreSizeLimit ? qt.MinSize : qt.MaxSize - (qt.MaxSize - qt.MinSize) * qtMaxWait); i--)
                        if (qt.Mode == AutohostMode.GameChickens || i % 2 == 0)
                        {
                            if (Party == null || (qt.Mode == AutohostMode.GameChickens && Party.UserNames.Count<=i) || Party.UserNames.Count == i / 2)
                                ret.Add(new ProposedBattle(i, this, qt, qt.EloCutOffExponent, allPlayers));
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
