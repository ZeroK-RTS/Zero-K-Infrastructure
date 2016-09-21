using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;

namespace ZkLobbyServer
{
    public class MatchMakerBattle: ServerBattle
    {
        public MatchMakerBattle(ZkLobbyServer server, MatchMaker.ProposedBattle bat): base(server, null)
        {
            IsMatchMakerBattle = true;
            EngineVersion = server.Engine;
            ModName = server.Game;
            FounderName = "MatchMaker #" + BattleID;
            Title = "MatchMaker " + BattleID;
            Mode = bat.Mode;
            MaxPlayers = bat.Size;

        
            foreach (var pe in bat.Players) Users[pe.Name] = new UserBattleStatus(pe.Name, pe.LobbyUser, Guid.NewGuid().ToString());

            ValidateAndFillDetails();

        }
    }
}