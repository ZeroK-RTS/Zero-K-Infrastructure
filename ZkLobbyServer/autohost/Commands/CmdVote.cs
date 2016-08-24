using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdVote : ServerBattleCommand
    {
        public override string Help => "<number> - votes for given option, e.g. !vote 1";
        public override string Shortcut => "vote";
        public override BattleCommandAccess Access => BattleCommandAccess.NoCheck;

        public override ServerBattleCommand Create() => new CmdVote();

        private int opt;

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (arguments != null) int.TryParse(arguments, out opt);
            return String.Empty;
        }

        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.RegisterVote(e, opt != 2);
        }
    }
}