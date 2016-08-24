// Contact: Jan Lichovník  licho@licho.eu, tel: +420 604 935 349,  www.itl.cz
// Last change by: licho  24.08.2016

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdResign : ServerBattleCommand
    {
        private int alliance;
        public override string Help => "starts a vote to resign game";
        public override string Shortcut => "resign";
        public override BattleCommandAccess Access => BattleCommandAccess.IngameVote;

        public override ServerBattleCommand Create() => new CmdResign();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (DateTime.UtcNow.Subtract(battle.spring.IngameStartTime ?? DateTime.Now).TotalSeconds < GlobalConst.MinDurationForElo)
            {
                battle.Respond(e, "You cannot resign so early");
                return null;
            }

            var voteStarter = battle.spring.StartContext?.Players.FirstOrDefault(x => x.Name == e.User && !x.IsSpectator);
            if (voteStarter != null)
            {
                alliance = voteStarter.AllyID;
                return $"Resign team {voteStarter.AllyID + 1}?";
            }
            return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            var s = battle.spring;
            if (s.IsRunning) foreach (var p in s.StartContext.Players.Where(x => x.AllyID == alliance && !x.IsSpectator)) s.ResignPlayer(p.Name);
            await battle.SayBattle($"Team {alliance + 1} resigned");
        }

        public override CommandExecutionRight RunPermissions(ServerBattle battle, string userName)
        {
            var ret = base.RunPermissions(battle, userName);

            // only people from same team can vote
            if (ret >= CommandExecutionRight.Vote)
            {
                if (battle.spring.IsRunning)
                {
                    var entry = battle.spring.StartContext.Players.FirstOrDefault(x => x.Name == userName);
                    if (entry != null && !entry.IsSpectator && entry.AllyID == alliance) return ret;
                }
            }
            return CommandExecutionRight.None;
        }
    }
}