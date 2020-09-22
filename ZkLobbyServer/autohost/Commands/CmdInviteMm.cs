using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdInviteMm : BattleCommand
    {
        private int minplayers;
        public override string Help => "invitemm - changes minimum number of players needed to enable automatic MM invites after each battle";
        public override string Shortcut => "invitemm";
        public override AccessType Access => AccessType.Admin;

        public override BattleCommand Create() => new CmdInviteMm();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (int.TryParse(arguments, out minplayers))
            {
                return string.Empty;
            }
            else return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.SwitchInviteMmPlayers(minplayers);
            await battle.SayBattle("Minimum players for automatic MM invites changed to " + minplayers);

        }
    }
}