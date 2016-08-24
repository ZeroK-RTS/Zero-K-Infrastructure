using System.Threading.Tasks;
using LobbyClient;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdHostsay : ServerBattleCommand
    {
        public override string Help => "says something as host, useful for !hostsay /nocost etc";
        public override string Shortcut => "hostsay";
        public override BattleCommandAccess Access => BattleCommandAccess.Ingame;

        public override ServerBattleCommand Create() => new CmdHostsay();

        private string cmd;
        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            cmd = arguments;
        }
        
        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (battle.spring.IsRunning)
            {
                battle.spring.SayGame(cmd);
            }
        }
    }
}
