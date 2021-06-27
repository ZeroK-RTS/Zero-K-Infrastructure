using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlasmaShared;

namespace LobbyClient
{
    [Message(Origin.Server)]
    public class MatchMakerSetup
    {
        public class Queue
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> Maps { get; set; } = new List<string>();
            public string Game { get; set; }

            public int MaxPartySize { get; set; }

            [JsonIgnore]
            public bool UseWinChanceLimit { get; set; }

            [JsonIgnore]
            public bool UseHandicap { get; set; }

            [JsonIgnore]
            public int MaxSize { get; set; }

            [JsonIgnore]
            public int MinSize { get; set; }

            [JsonIgnore]
            public double EloCutOffExponent { get; set; }

            [JsonIgnore]
            public AutohostMode Mode { get; set; }

            [JsonIgnore]
            public List<string> SafeMaps { get; set; }

            public override bool Equals(object obj)
            {
                return Name.Equals((obj as Queue)?.Name);
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
        }
        public List<Queue> PossibleQueues { get; set; }
    }

    [Message(Origin.Client)]
    public class MatchMakerQueueRequest
    {
        public List<string> Queues { get; set; } = new List<string>();
    }


    [Message(Origin.Server)]
    public class MatchMakerStatus
    {
        public bool MatchMakerEnabled => JoinedQueues?.Count > 0;
        public List<string> JoinedQueues { get; set; } = new List<string>();
        public Dictionary<string, int> QueueCounts { get; set; } = new Dictionary<string, int>();
        public int? CurrentEloWidth { get; set; }
        public DateTime? JoinedTime { get; set; }
        public int? BannedSeconds { get; set; }
        public List<string> InstantStartQueues { get; set; } = new List<string>();

        public Dictionary<string, int> IngameCounts { get; set; } = new Dictionary<string, int>();

        public int UserCount { get; set; }
        public int UserCountDiscord { get; set; }
    }



    [Message(Origin.Server)]
    public class AreYouReady
    {
        public double MinimumWinChance { get; set; } = -1;
        public bool QuickPlay { get; set; } = false;
        public int SecondsRemaining { get; set; } = 10;
    }

    [Message(Origin.Server)]
    public class AreYouReadyUpdate
    {
        public bool ReadyAccepted { get; set; }
        public bool LikelyToPlay { get; set; }
        public Dictionary<string, int> QueueReadyCounts { get; set; } = new Dictionary<string, int>();
        public int? YourBattleSize { get; set; }
        public int? YourBattleReady { get; set; }
    }

    [Message(Origin.Server)]
    public class AreYouReadyResult
    {
        public bool IsBattleStarting { get; set; }
        public bool AreYouBanned { get; set; }
    }


    [Message(Origin.Client)]
    public class AreYouReadyResponse
    {
        public bool Ready { get; set; }
    }

}
