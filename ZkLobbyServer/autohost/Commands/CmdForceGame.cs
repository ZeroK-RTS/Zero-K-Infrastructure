using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdForceGame : BattleCommand
    {
        public override string Help => "<exact name> - changes to any rapid accessible third party game, exact name needed";
        public override string Shortcut => "forcegame";
        public override AccessType Access => AccessType.NotIngameNotAutohost;

        public override BattleCommand Create() => new CmdForceGame();

        private string gameName;
        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            this.gameName = arguments;


            if (string.IsNullOrEmpty(arguments)) {
                battle.Respond(e, "Please specify game name.");
                return null;
            }

            return $"Force change game to ?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (gameName != null)
            {
                await battle.SwitchGame(gameName);
                await battle.SayBattle("Changing game to " + gameName);
                battle.SwitchDefaultGame(false);
                await battle.SayBattle("This host will no longer update its game automatically");
            }
            
        }
    }
}
