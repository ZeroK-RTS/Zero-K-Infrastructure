using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZeroKWeb.SpringieInterface;
using ZkData;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdStart : ServerBattleCommand
    {
        public override string Help => "starts the game";
        public override string Shortcut => "start";
        public override BattleCommandAccess Access => BattleCommandAccess.NotIngame;

        public override ServerBattleCommand Create() => new CmdStart();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            battle.RunCommand<CmdRing>(e);
            return $"start the game?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.StartGame();

        }
    }
}