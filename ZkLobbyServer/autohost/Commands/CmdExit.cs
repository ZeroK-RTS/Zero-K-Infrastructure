using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdExit : BattleCommand
    {
        public override string Help => "exits the game";
        public override string Shortcut => "exit";
        public override AccessType Access => AccessType.Ingame;

        public override BattleCommand Create() => new CmdExit();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            return "do you want to exit the game?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (battle.spring.IsRunning)
            {
                await battle.SayBattle("exiting game");
                battle.spring.ExitGame();
            }
        }
    }
}