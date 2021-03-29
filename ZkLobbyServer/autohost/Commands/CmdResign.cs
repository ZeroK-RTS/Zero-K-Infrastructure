// Contact: Jan Lichovník  licho@licho.eu, tel: +420 604 935 349,  www.itl.cz
// Last change by: licho  24.08.2016

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdResign : BattleCommand
    {
        private int? alliance;
        public override string Help => "starts a vote to resign game";
        public override string Shortcut => "resign";
        public override AccessType Access => AccessType.IngameVote;

        public override BattleCommand Create() => new CmdResign();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (DateTime.UtcNow.Subtract(battle.spring.IngameStartTime ?? DateTime.UtcNow).TotalSeconds < GlobalConst.MinDurationForElo)
            {
                battle.Respond(e, "You cannot resign so early");
                return null;
            }

            var voteStarter = battle.spring.LobbyStartContext?.Players.FirstOrDefault(x => x.Name == e.User && !x.IsSpectator);
            if (voteStarter != null)
            {
                alliance = voteStarter.AllyID;
                return $"Resign team {voteStarter.AllyID + 1}?";
            }
            else
            {
                battle.Respond(e, "Only players can invoke this");
            }
            return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            var s = battle.spring;
            if (s.IsRunning) foreach (var p in s.LobbyStartContext.Players.Where(x => x.AllyID == alliance && !x.IsSpectator)) s.ResignPlayer(p.Name);
            await battle.SayBattle($"Team {alliance + 1} resigned");
        }

        public override RunPermission GetRunPermissions(ServerBattle battle, string userName, out string reason)
        {
            var ret = base.GetRunPermissions(battle, userName, out reason);

            // only people from same team can vote
            if (ret >= RunPermission.Vote)
            {
                if (battle.spring.IsRunning)
                {
                    var entry = battle.spring.LobbyStartContext.Players.FirstOrDefault(x => x.Name == userName);
                    if (entry != null && !entry.IsSpectator && (alliance == null || entry.AllyID == alliance)) return ret;

                    // if player is dead, he cannot vote
                    if (!battle.spring.Context.ActualPlayers.Any(x=>x.Name == userName && x.LoseTime == null)) return RunPermission.None;
                }
            }
            return RunPermission.None;
        }

        public override int GetPollWinMargin(ServerBattle battle, int numVoters)
        {
            // We might want different logic to determine the success of a resign vote based on the number of players.
            // For now just require one more vote than needed for all game sizes, this requires a unanimous vote for up to 4v4 inclusive.
            return 2;
        }
    }
}