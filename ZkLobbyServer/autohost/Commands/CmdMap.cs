using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdMap : BattleCommand
    {
        private Resource map;
        private Resource alternativeMap;
        public override string Help => "[<filters>..] - changes map, e.g. !map altor div changes map to Altored Divide";
        public override string Shortcut => "map";
        public override AccessType Access => AccessType.NotIngame;

        public override BattleCommand Create() => new CmdMap();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            map = string.IsNullOrEmpty(arguments)
                ? MapPicker.GetRecommendedMap(battle.GetContext())
                : MapPicker.FindResources(ResourceType.Map, arguments, battle.MinimalMapSupportLevel).FirstOrDefault();


            if (map == null)
            {
                battle.Respond(e, "Cannot find such map.");
                return null;
            }
            else if (!string.IsNullOrEmpty(arguments) && map.MapSupportLevel < MapSupportLevel.Supported)
            {
                alternativeMap = MapPicker.FindResources(ResourceType.Map, arguments, MapSupportLevel.Supported).FirstOrDefault();
            }


            if (map.MapSupportLevel >= MapSupportLevel.Supported)
            {
                return $"Change map to {map.InternalName} {GlobalConst.BaseSiteUrl}/Maps/Detail/{map.ResourceID} ?";
            }
            else
            {
                return $"Change to UNSUPPORTED map {map.InternalName} {GlobalConst.BaseSiteUrl}/Maps/Detail/{map.ResourceID} ?";
            }
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (map != null)
            {
                await battle.SwitchMap(map.InternalName);
                await battle.SayBattle("changing map to " + map.InternalName);
                if (map.MapSupportLevel < MapSupportLevel.Supported) await battle.SayBattle($"This map is not officially supported!");
                if (alternativeMap != null) await battle.SayBattle($"Did you mean {alternativeMap.InternalName} {GlobalConst.BaseSiteUrl}/Maps/Detail/{alternativeMap.ResourceID}?");
            }
            
        }
    }
}