using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdCheats : BattleCommand
    {
        public override string Help => "enables/disables cheats in game";
        public override string Shortcut => "cheats";
        public override AccessType Access => AccessType.Ingame;

        public override BattleCommand Create() => new CmdCheats();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            return "Do you want to enable cheats?";
        }
        
        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (battle.spring.IsRunning)
            {
                battle.spring.SayGame("/cheat");
                await battle.SayBattle("Cheats!");
            }
        }
    }
}