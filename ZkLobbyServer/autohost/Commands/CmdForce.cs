using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdForce : BattleCommand
    {
        public override string Help => "forces game start (skips waiting for players)";
        public override string Shortcut => "force";
        public override AccessType Access => AccessType.Ingame;

        public override BattleCommand Create() => new CmdForce();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            return $"do you want to force start?";
        }
        
        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (battle.spring.IsRunning)
            {
                battle.spring.ForceStart();
                await battle.SayBattle($"Force starting game");
            }
        }
    }
}