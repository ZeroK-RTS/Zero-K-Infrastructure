using System.Threading.Tasks;
using LobbyClient;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdCheats : ServerBattleCommand
    {
        public override string Help => "enables/disables cheats in game";
        public override string Shortcut => "cheats";
        public override BattleCommandAccess Access => BattleCommandAccess.Ingame;

        public override ServerBattleCommand Create() => new CmdCheats();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            return "do you want to enable cheats?";
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