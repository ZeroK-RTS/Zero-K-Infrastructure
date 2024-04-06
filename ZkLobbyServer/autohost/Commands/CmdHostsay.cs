using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdHostsay : BattleCommand
    {
        public override string Help => "says something as host, useful for !hostsay /nocost etc";
        public override string Shortcut => "hostsay";
        public override AccessType Access => AccessType.AdminOrRoomFounder;

        public override BattleCommand Create() => new CmdHostsay();

        private string cmd;
        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            cmd = arguments;
            return $"Do you want to host say {cmd} ?";
        }
        
        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (battle.spring.IsRunning)
            {
                battle.spring.SayGame(cmd);
                await battle.SayBattle($"Host executing {cmd}");
            }
        }
    }
}
