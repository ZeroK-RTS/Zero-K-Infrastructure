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
            public List<string> MapList = new List<string>();

            public Dictionary<string, string> ModOptions = new Dictionary<string, string>();
        }

        private TourneyPrototype prototype;

        
        public TourneyBattle(ZkLobbyServer server, TourneyPrototype prototype) : base(server, null)
        {
            this.prototype = prototype;
            IsMatchMakerBattle = true;
            EngineVersion = server.Engine;
            ModName = server.Game;
            FounderName = "Tourney #" + BattleID;
            Title =  prototype.Title;
            Mode = AutohostMode.None;
            MapName = prototype.MapList.FirstOrDefault();
            MaxPlayers = prototype.TeamPlayers.Sum(x=>x.Count);


            for (int teamNumber = 0; teamNumber < prototype.TeamPlayers.Count; teamNumber++)
            {
                var team = prototype.TeamPlayers[teamNumber];
                foreach (var pe in team) Users[pe] = new UserBattleStatus(pe, server.ConnectedUsers.Get(pe)?.User, GenerateClientScriptPassword(pe)) { AllyNumber = teamNumber };
            }

            ModOptions = prototype.ModOptions;
            ModOptions["mutespec"] = "mute";

            ValidateAndFillDetails();
        }

        public override void ValidateBattleStatus(UserBattleStatus ubs)
        {

            for (int teamNumber = 0; teamNumber < prototype.TeamPlayers.Count; teamNumber++)
            {
                var team = prototype.TeamPlayers[teamNumber];
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