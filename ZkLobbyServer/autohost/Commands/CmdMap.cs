using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdMap : BattleCommand
    {
        public Resource Map { get; private set; }
        private Resource alternativeMap;
        public override string Help => "[<filters>..] - changes map, e.g. !map altor div changes map to Altored Divide";
        public override string Shortcut => "map";
        public override AccessType Access => AccessType.NotIngame;

        public override BattleCommand Create() => new CmdMap();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            Map = string.IsNullOrEmpty(arguments)
                ? MapPicker.GetRecommendedMap(battle.GetContext(), (battle.MinimalMapSupportLevel > MapSupportLevel.Featured) ? battle.MinimalMapSupportLevel : MapSupportLevel.Featured)
                : MapPicker.FindResources(ResourceType.Map, arguments, battle.MinimalMapSupportLevel, true).FirstOrDefault();


            if (Map == null)
            {
                var unsupportedMap = MapPicker.FindResources(ResourceType.Map, arguments, MapSupportLevel.None).FirstOrDefault();
                if (unsupportedMap != null)
                {
                    if (battle.IsAutohost)
                    {
                        battle.Respond(e, $"The map {unsupportedMap.InternalName} {GlobalConst.BaseSiteUrl}/Maps/Detail/{unsupportedMap.ResourceID} is not available on this autohost. Play it in a player hosted battle.");
                    }
                    else
                    {
                        battle.Respond(e, $"The map {unsupportedMap.InternalName} {GlobalConst.BaseSiteUrl}/Maps/Detail/{unsupportedMap.ResourceID} is not supported. Unsupported maps can only be played on passworded hosts.");
                    }
                }
                else
                {
                    battle.Respond(e, "Cannot find such a map.");
                }
                return null;
            }
            else if (Map.InternalName == battle.MapName)
            {
                battle.Respond(e, "Already on this map.");
                return null;
            }
            else if (!string.IsNullOrEmpty(arguments) && Map.MapSupportLevel < MapSupportLevel.Supported)
            {
                alternativeMap = MapPicker.FindResources(ResourceType.Map, arguments, MapSupportLevel.Supported, true).FirstOrDefault();
            }


            if (Map.MapSupportLevel >= MapSupportLevel.Supported)
            {
                return $"Change map to {Map.InternalName} {GlobalConst.BaseSiteUrl}/Maps/Detail/{Map.ResourceID} ?";
            }
            else
            {
                return $"Change to UNSUPPORTED map {Map.InternalName} {GlobalConst.BaseSiteUrl}/Maps/Detail/{Map.ResourceID} ?";
            }
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (Map != null)
            {
                await battle.SwitchMap(Map.InternalName);
                await battle.SayBattle("changing map to " + Map.InternalName);
                if (Map.MapSupportLevel < MapSupportLevel.Supported) await battle.SayBattle($"This map is not officially supported!");
                if (alternativeMap != null) await battle.SayBattle($"Did you mean {alternativeMap.InternalName} {GlobalConst.BaseSiteUrl}/Maps/Detail/{alternativeMap.ResourceID}?");
            }
            
        }
    }
}
