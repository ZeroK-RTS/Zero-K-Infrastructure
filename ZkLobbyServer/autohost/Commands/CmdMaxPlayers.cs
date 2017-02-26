using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdMaxPlayers : BattleCommand
    {
        private int cnt;
        public override string Help => "count - changes room size, e.g. !maxplayers 10";
        public override string Shortcut => "maxplayers";
        public override AccessType Access => AccessType.NotIngame;

        public override BattleCommand Create() => new CmdMaxPlayers();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (int.TryParse(arguments, out cnt) && cnt > 1)
            {
                return $"Change max players to {cnt}?";
            }
            else return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (cnt > 0)
            {
                await battle.SwitchMaxPlayers(cnt);
                await battle.SayBattle("Max players changed to " + cnt);
            }

        }
    }
}