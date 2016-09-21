using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class MatchMakerBattle : ServerBattle
    {
        private MatchMaker.ProposedBattle prototype;

        public MatchMakerBattle(ZkLobbyServer server, MatchMaker.ProposedBattle bat) : base(server, null)
        {
            IsMatchMakerBattle = true;
            EngineVersion = server.Engine;
            ModName = server.Game;
            FounderName = "MatchMaker #" + BattleID;
            Title = "MatchMaker " + BattleID;
            Mode = bat.Mode;
            MaxPlayers = bat.Size;
            prototype = bat;

            foreach (var pe in bat.Players) Users[pe.Name] = new UserBattleStatus(pe.Name, pe.LobbyUser, Guid.NewGuid().ToString());

            ValidateAndFillDetails();
        }

        public override void ValidateBattleStatus(UserBattleStatus ubs)
        {
            if (prototype.Players.Any(y => y.Name == ubs.Name))
            {
                ubs.IsSpectator = false;
                ubs.AllyNumber = 0;
            }
            else
            {
                ubs.IsSpectator = true;
            }
        }

        protected override async Task OnSpringExited(Spring.SpringBattleContext springBattleContext)
        {
            await base.OnSpringExited(springBattleContext);
            isZombie = true;
            await SayBattle($"This room is now disabled, please join a new game");
            await SwitchPassword(FounderName);
        }
    }
}