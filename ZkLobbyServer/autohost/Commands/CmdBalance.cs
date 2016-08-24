using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;
using ZkData;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdBalance : ServerBattleCommand
    {
        private Resource game;
        public override string Help => "[<teams>] - puts people into teams, respecting their clans and skill if possible";
        public override string Shortcut => "balance";
        public override BattleCommandAccess Access => BattleCommandAccess.NotIngame;

        public override ServerBattleCommand Create() => new CmdBalance();
        private int? teamCount;

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (battle.Mode != AutohostMode.None)
            {
                battle.Respond(e, $"Balance is only for custom hosts, this host is {battle.Mode.Description()}");
                return null;
            }
            if (arguments != null)
            {
                int tc;
                if (int.TryParse(arguments, out tc)) teamCount = tc;
            }

            return $"Do you want to balance {teamCount}";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            battle.RunServerBalance(false, teamCount, null);
            await battle.SayBattle("Teams were balanced");
        }
    }
}