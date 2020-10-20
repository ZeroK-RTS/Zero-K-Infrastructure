using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdMaxEvenPlayers : BattleCommand
    {
        private int cnt;
        public override string Help => "count - if there are <= maxeven players in a room, even balance will be enforced, e.g. !maxevenplayers 10";
        public override string Shortcut => "maxevenplayers";
        public override AccessType Access => AccessType.NotIngameNotAutohost;

        public override BattleCommand Create() => new CmdMaxEvenPlayers();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (int.TryParse(arguments, out cnt) && cnt > 1)
            {
                return $"Change max even players to {cnt}?";
            }
            else return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (cnt > 0)
            {
                await battle.SwitchMaxEvenPlayers(cnt);
                await battle.SayBattle("Max even players changed to " + cnt);
            }

        }
    }
}