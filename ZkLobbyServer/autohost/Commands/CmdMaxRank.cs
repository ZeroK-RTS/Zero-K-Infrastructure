using System.Threading.Tasks;
using LobbyClient;
using Ratings;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdMaxRank : BattleCommand
    {
        private int rank;
        public override string Help => "rank - changes rank limit for players, e.g. !maxrank 5";
        public override string Shortcut => "maxrank";
        public override AccessType Access => AccessType.Admin;

        public override BattleCommand Create() => new CmdMaxRank();

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
                await battle.SwitchMaxRank(rank);
                await battle.SayBattle("Max rank changed to " + Ranks.RankNames[rank]);
            }
        }
    }
}