using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdMaxLevel : BattleCommand
    {
        private int lvl;
        public override string Help => "level - changes level limit for players, e.g. !maxlevel 50";
        public override string Shortcut => "maxlevel";
        public override AccessType Access => AccessType.NotIngameNotAutohost;

        public override BattleCommand Create() => new CmdMaxLevel();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (int.TryParse(arguments, out lvl))
            {
                return string.Empty;
            }
            else return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.SwitchMaxLevel(lvl);
            await battle.SayBattle("Max level changed to " + lvl);
            await battle.SayBattle($"Warning: This command will have no effect if there are more than {DynamicConfig.Instance.MaximumStatLimitedBattlePlayers} players in the room!");

        }
    }
}