using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdPassword : BattleCommand
    {
        private string pwd;
        public override string Help => "<newpassword> - sets room password";
        public override string Shortcut => "password";
        public override AccessType Access => AccessType.NotIngameNotAutohost;

        public override BattleCommand Create() => new CmdPassword();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            pwd=arguments;
            if (string.IsNullOrEmpty(pwd)) return "remove password?";
            return $"change password to {pwd} ?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.SwitchPassword(pwd);
            await battle.SayBattle(string.IsNullOrEmpty(pwd) ? "battle password removed" : "battle room password changed");
        }
    }
}