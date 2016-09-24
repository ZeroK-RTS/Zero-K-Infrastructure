using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class MatchMakerBattle : ServerBattle
    {
        public MatchMaker.ProposedBattle Prototype { get; private set; }

        public MatchMakerBattle(ZkLobbyServer server, MatchMaker.ProposedBattle bat) : base(server, null)
        {
            IsMatchMakerBattle = true;
            EngineVersion = server.Engine;
            ModName = server.Game;
            FounderName = "MatchMaker #" + BattleID;
            Title = "MatchMaker " + BattleID;
            Mode = bat.QueueType.Mode;
            MaxPlayers = bat.Size;
            Prototype = bat;

            foreach (var pe in bat.Players) Users[pe.Name] = new UserBattleStatus(pe.Name, pe.LobbyUser, Guid.NewGuid().ToString());

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

        protected override async Task OnDedicatedExited(SpringBattleContext springBattleContext)
        {
            await base.OnDedicatedExited(springBattleContext);
            isZombie = true;
            await SayBattle($"This room is now disabled, please join a new game");
            await SwitchPassword(FounderName);
        }
    }
}