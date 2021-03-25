using System;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdFlip : BattleCommand
    {
        public override string Help => "Flips a coin";
        public override string Shortcut => "flip";
        public override AccessType Access => AccessType.NoCheck;

        public override BattleCommand Create() => new CmdFlip();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            return string.Empty;
        }

        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            string result = new Random().Next(2) == 1 ? "heads" : "tails";
            await battle.SayBattle($"Flipped a coin, got {result}");
        }
    }
}
