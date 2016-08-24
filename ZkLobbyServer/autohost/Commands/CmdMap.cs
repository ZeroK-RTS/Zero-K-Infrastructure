using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZeroKWeb.SpringieInterface;
using ZkData;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdMap : ServerBattleCommand
    {
        private Resource map;
        public override string Help => "[<filters>..] - changes server map, eg. !map altor div";
        public override string Shortcut => "map";
        public override BattleCommandAccess Access => BattleCommandAccess.NotIngame;

        public override ServerBattleCommand Create() => new CmdMap();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            map = !string.IsNullOrEmpty(arguments)
                ? MapPicker.FindResources(ResourceType.Map, arguments).FirstOrDefault()
                : MapPicker.GetRecommendedMap(battle.GetContext());

            if (map == null)
            {
                battle.Respond(e, "Cannot find such map.");
                return null;
            }

            return $"Change map to {map.InternalName} {GlobalConst.BaseSiteUrl}/Maps/Detail/{map.ResourceID} ?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (battle.spring.IsRunning)
            {
                await battle.Respond(e, "Cannot change map while the game is running");
                return;
            }

            if (map != null)
            {
                await battle.SwitchMap(map.InternalName);
                await battle.SayBattle("changing map to " + map.InternalName);
            }
            
        }
    }
}