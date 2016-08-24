using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdExit : ServerBattleCommand
    {
        public override string Help => "exits the game";
        public override string Shortcut => "exit";
        public override BattleCommandAccess Access => BattleCommandAccess.Ingame;

        public override ServerBattleCommand Create() => new CmdExit();

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