using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer {
    public class TourneyBattle : ServerBattle
    {
        public class TourneyPrototype
        {
            public string Title;
            public List<List<string>> TeamPlayers = new List<List<string>>();

            public Dictionary<string, string> ModOptions = new Dictionary<string, string>();
        }

        public TourneyPrototype Prototype;

        
        public TourneyBattle(ZkLobbyServer server, TourneyPrototype prototype) : base(server, null)
        {
            this.Prototype = prototype;
            IsMatchMakerBattle = false;
            ApplicableRating = RatingCategory.MatchMaking;
            EngineVersion = server.Engine;
            ModName = server.Game;
            FounderName = $"Tourney #{BattleID}";
            Title =  prototype.Title;
            Mode = prototype.TeamPlayers.Max(x => x.Count) == 1 ? AutohostMode.Game1v1 : AutohostMode.None;
            MaxPlayers = prototype.TeamPlayers.Sum(x=>x.Count);
            ModOptions = prototype.ModOptions;
            ModOptions["mutespec"] = "mute";
            ModOptions["mutelobby"] = "mute";
            ModOptions["allyreclaim"] = "1";

            ValidateAndFillDetails();
        }

        public override void ValidateBattleStatus(UserBattleStatus ubs)
        {

            for (int teamNumber = 0; teamNumber < Prototype.TeamPlayers.Count; teamNumber++)
            {
                var team = Prototype.TeamPlayers[teamNumber];
                if (team.Any(x => x == ubs.Name))
                {
                    ubs.IsSpectator = false;
                    ubs.AllyNumber = teamNumber;
                    return;
                }
            }
            ubs.IsSpectator = true;
        }

    }
}
