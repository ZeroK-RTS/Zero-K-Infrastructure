using System.Threading.Tasks;
using LobbyClient;
using ZkData;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdMaxPlayers : ServerBattleCommand
    {
        private int cnt;
        public override string Help => "count - changes room size, e.g. !maxplayers 10";
        public override string Shortcut => "maxplayers";
        public override BattleCommandAccess Access => BattleCommandAccess.NotIngame;

        public override ServerBattleCommand Create() => new CmdMaxPlayers();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (int.TryParse(arguments, out cnt) && cnt > 1)
            {
                return $"Change title to {cnt}?";
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