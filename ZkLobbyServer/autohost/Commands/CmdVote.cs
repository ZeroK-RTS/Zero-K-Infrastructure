using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdVote : BattleCommand
    {
        public override string Help => "<number> - votes for given option, e.g. !vote 1";
        public override string Shortcut => "vote";
        public override AccessType Access => AccessType.NoCheck;

        public override BattleCommand Create() => new CmdVote();

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