using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZeroKWeb;
using ZkData;

namespace ZkLobbyServer
{
    public class PlanetWarsServerBattle: AutoClosedServerBatle
    {
        private PlanetWarsMatchMaker.AttackOption prototype;

        public PlanetWarsServerBattle(ZkLobbyServer server, PlanetWarsMatchMaker.AttackOption option): base(server, null)
        {
            IsMatchMakerBattle = true;
            IsCompetitiveBattle = false;
            EngineVersion = server.Engine;
            ModName = server.Game;
            FounderName = "PlanetWars #" + BattleID;
            Title = "PlanetWars " + BattleID;
            Mode = AutohostMode.Planetwars;
            MapName = option.Map;
            MaxPlayers = option.TeamSize*2;
            prototype = option;

            foreach (var pe in option.Attackers) Users[pe] = new UserBattleStatus(pe, server.ConnectedUsers.Get(pe)?.User, GenerateClientScriptPassword(pe)) {AllyNumber = 0};
            foreach (var pe in option.Defenders) Users[pe] = new UserBattleStatus(pe, server.ConnectedUsers.Get(pe)?.User, GenerateClientScriptPassword(pe)) { AllyNumber = 1 };

            if (ModOptions == null) ModOptions = new Dictionary<string, string>();

            ModOptions["mutespec"] = "mute";

            ValidateAndFillDetails();
        }

        public override void ValidateBattleStatus(UserBattleStatus ubs)
        {
            if (prototype.Attackers.Any(y => y == ubs.Name))
            {
                ubs.IsSpectator = false;
                ubs.AllyNumber = 0;
            } 
            else if (prototype.Defenders.Any(y => y == ubs.Name))
            {
                ubs.IsSpectator = false;
                ubs.AllyNumber = 1;
            }
            else
            {
                ubs.IsSpectator = true;
            }
        }
    }
}