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
        public override string Help => "[<filters>..] - changes map, e.g. !map altor div changes map to Altored Divide";
        public override string Shortcut => "map";
        public override AccessType Access => AccessType.NotIngame;

        public override BattleCommand Create() => new CmdMap();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            map = string.IsNullOrEmpty(arguments)
                ? MapPicker.GetRecommendedMap(battle.GetContext())
                : MapPicker.FindResources(ResourceType.Map, arguments).FirstOrDefault();
            
            if (map == null)
            {
                battle.Respond(e, "Cannot find such map.");
                return null;
            }

            return $"Change map to {map.InternalName} {GlobalConst.BaseSiteUrl}/Maps/Detail/{map.ResourceID} ?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (map != null)
            {
                await battle.SwitchMap(map.InternalName);
                await battle.SayBattle("changing map to " + map.InternalName);
            }
            
        }
    }
}