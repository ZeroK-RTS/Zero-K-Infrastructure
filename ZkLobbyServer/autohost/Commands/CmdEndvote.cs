using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdEndvote : BattleCommand
    {
        public override string Help => "- ends current poll";
        public override string Shortcut => "endvote";
        public override AccessType Access => AccessType.Anywhere;
        public override BattleCommand Create() => new CmdEndvote();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            return string.Empty;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            battle.StopVote(e);
            await battle.SayBattle("poll cancelled");
        }

        public override RunPermission GetRunPermissions(ServerBattle battle, string userName, out string reason)
        {
            reason = "";
            if (battle.ActivePoll?.Creator?.User == userName) return RunPermission.Run; // can end own poll
            var ret = base.GetRunPermissions(battle, userName, out reason);
            if (ret == RunPermission.Vote) return RunPermission.None; // do not allow vote (ever)
            return ret;
        }
    }
}