using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdStart : BattleCommand
    {
        public override string Help => "starts the game";
        public override string Shortcut => "start";
        public override AccessType Access => AccessType.NotIngame;

        public override BattleCommand Create() => new CmdStart();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            battle.RunCommandDirectly<CmdRing>(e);
            return $"Start the game?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.StartGame();

        }
    }
}
