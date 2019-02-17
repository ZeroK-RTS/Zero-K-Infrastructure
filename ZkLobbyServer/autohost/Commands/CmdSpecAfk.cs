using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdSpecAfk : BattleCommand
    {
        public override AccessType Access => AccessType.NotIngame;
        public override string Help => "[<filters>..] - spectates AFK players";
        public override string Shortcut => "specafk";

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            return $"do you want to spectate AFK?";
        }

        public override BattleCommand Create() => new CmdSpecAfk();


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            foreach (var usr in battle.Users.Values.Where(x => !x.IsSpectator && x.LobbyUser.IsAway)) await battle.Spectate(usr.Name);

            await battle.SayBattle($"forcing AFK to spectator");
        }

        public override RunPermission GetRunPermissions(ServerBattle battle, string userName, out string reason)
        {
            var ret = base.GetRunPermissions(battle, userName, out reason);
            if (ret >= RunPermission.Vote) return RunPermission.Run;
            return ret;
        }
    }
}