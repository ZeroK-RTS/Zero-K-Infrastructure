using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdMaxElo : BattleCommand
    {
        private int elo;
        public override string Help => "elo - changes elo limit for players, e.g. !maxelo 1600";
        public override string Shortcut => "maxelo";
        public override AccessType Access => AccessType.NotIngameNotAutohost;

        public override BattleCommand Create() => new CmdMaxElo();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (int.TryParse(arguments, out elo))
            {
                return string.Empty;
            }
            else return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.SwitchMaxElo(elo);
            await battle.SayBattle("Max elo changed to " + elo);
            await battle.SayBattle($"Warning: This command will have no effect if there are more than {DynamicConfig.Instance.MaximumStatLimitedBattlePlayers} players in the room!");

        }
    }
}