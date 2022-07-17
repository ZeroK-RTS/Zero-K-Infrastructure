using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdListMapOptions : BattleCommand
    {
        public override string Help => "lists all map options";
        public override string Shortcut => "listmapoptions";
        public override AccessType Access => AccessType.NoCheck;

        public override BattleCommand Create() => new CmdListMapOptions();

        public override string Arm(ServerBattle battle, Say e, string arguments = null) => String.Empty;

        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            var map = battle.HostedMapInfo;
            if (map== null || map.Options?.Any() != true) await battle.Respond(e, "this map has no map options");
            else foreach (var opt in map.Options) await battle.Respond(e, opt.ToString());
        }
    }
}