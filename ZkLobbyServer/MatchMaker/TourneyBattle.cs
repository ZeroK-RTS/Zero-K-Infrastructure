using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer {
    public class TourneyBattle : ServerBattle
    {
        public class TourneyPrototype
        {
            public string Title;
            public string FounderName;
            public List<List<string>> TeamPlayers = new List<List<string>>();

            public Dictionary<string, string> ModOptions = new Dictionary<string, string>();
            public Dictionary<string, string> MapOptions = new Dictionary<string, string>();
        }

        public TourneyPrototype Prototype;

        
        public TourneyBattle(ZkLobbyServer server, TourneyPrototype prototype) : base(server, null)
        {
            this.Prototype = prototype;
            IsMatchMakerBattle = false;
            ApplicableRating = RatingCategory.MatchMaking;
            EngineVersion = server.Engine;
            ModName = server.Game;
            FounderName = prototype.FounderName ?? $"Tourney #{BattleID}";
            Title =  prototype.Title;
            Mode = prototype.TeamPlayers.Max(x => x.Count) == 1 ? AutohostMode.Game1v1 : AutohostMode.None;
            MaxPlayers = prototype.TeamPlayers.Sum(x=>x.Count);
            ModOptions = prototype.ModOptions;
            MapOptions = prototype.MapOptions;

            SetCompetitiveModoptions();
            ModOptions["allyreclaim"] = "1"; // even more competitive than the above

            ValidateAndFillDetails();
        }

        public override async Task CheckCloseBattle()
        {
            //Don't close tourney battles automatically
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
