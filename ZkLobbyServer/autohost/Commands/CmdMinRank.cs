using System.Threading.Tasks;
using LobbyClient;
using Ratings;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdMinRank : BattleCommand
    {
        private int rank;
        public override string Help => "rank - changes rank limit for players, e.g. !minrank 5";
        public override string Shortcut => "minrank";
        public override AccessType Access => AccessType.Admin;

        public override BattleCommand Create() => new CmdMinRank();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (int.TryParse(arguments, out rank))
            {
                return string.Empty;
            }
            else return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (rank < Ranks.Percentiles.Length && rank >= 0)
            {
                await battle.SwitchMinRank(rank);
                await battle.SayBattle("Min rank changed to " + Ranks.RankNames[rank]);
            }
        }
    }
}