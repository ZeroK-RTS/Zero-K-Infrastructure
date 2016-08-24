using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdEndvote : ServerBattleCommand
    {
        public override string Help => "- ends current poll";
        public override string Shortcut => "endvote";
        public override BattleCommandAccess Access => BattleCommandAccess.Anywhere;
        public override ServerBattleCommand Create() => new CmdEndvote();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            return string.Empty;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            battle.StopVote(e);
            await battle.SayBattle("poll cancelled");
        }

        public override CommandExecutionRight RunPermissions(ServerBattle battle, string userName)
        {
            var ret = base.RunPermissions(battle, userName);
            if (ret == CommandExecutionRight.Vote) return CommandExecutionRight.None; // do not allow vote (ever)
            return ret;
        }
    }
}