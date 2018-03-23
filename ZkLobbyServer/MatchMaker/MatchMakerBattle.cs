using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using PlasmaShared;

namespace ZkLobbyServer
{
    public class MatchMakerBattle : AutoClosedServerBatle
    {
        public MatchMaker.ProposedBattle Prototype { get; private set; }

        public MatchMakerBattle(ZkLobbyServer server, MatchMaker.ProposedBattle bat, string mapname) : base(server, null)
        {
            IsMatchMakerBattle = true;
            EngineVersion = server.Engine;
            ModName = server.Game;
            FounderName = "MatchMaker #" + BattleID;
            Title = "MatchMaker " + BattleID;
            Mode = bat.QueueType.Mode;
            MaxPlayers = bat.Size;
            Prototype = bat;
            MapName = mapname;

            foreach (var pe in bat.Players) Users[pe.Name] = new UserBattleStatus(pe.Name, pe.LobbyUser, GenerateClientScriptPassword(pe.Name));
            
            if (ModOptions == null) ModOptions = new Dictionary<string, string>();

            // hacky way to send some extra start setup data
            if (bat.QueueType.Mode != AutohostMode.GameChickens) ModOptions["mutespec"] = "mute";
            ModOptions["MatchMakerType"] = bat.QueueType.Name;
            ModOptions["MinSpeed"] = "1";
            ModOptions["MaxSpeed"] = "1";

            ValidateAndFillDetails();
        }

        public override void ValidateBattleStatus(UserBattleStatus ubs)
        {
            if (Prototype.Players.Any(y => y.Name == ubs.Name))
            {
                ubs.IsSpectator = false;
                ubs.AllyNumber = 0;
            }
            else
            {
                ubs.IsSpectator = true;
            }
        }
    }
}
