using System;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdMinMapSupportLevel : BattleCommand
    {
        private int level;
        public override string Help => "level - changes minimum map support level (0-3), only for autohosts";
        public override string Shortcut => "minmapsupportlevel";
        public override AccessType Access => AccessType.Admin;

        public override BattleCommand Create() => new CmdMinMapSupportLevel();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (int.TryParse(arguments, out level))
            {
                return string.Empty;
            }
            else return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (Enum.IsDefined(typeof(MapSupportLevel), level)) {
                await battle.SwitchMinMapSupportLevel(((MapSupportLevel)level));
                await battle.SayBattle("Minimum map support level changed to " + ((MapSupportLevel)level).ToString());
            }

        }
    }
}