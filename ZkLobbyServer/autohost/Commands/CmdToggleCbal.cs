using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaDownloader;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdToggleCbal : BattleCommand
    {
        public override string Help => "Enables or disables balancing with respect to clans";
        public override string Shortcut => "togglecbal";
        public override AccessType Access => AccessType.Admin;

        public override BattleCommand Create() => new CmdToggleCbal();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (battle.IsMatchMakerBattle || battle.ApplicableRating != RatingCategory.Casual)
            {
                battle.Respond(e, "Only casual battles may have clan balance toggled");
                return null;
            }
            return string.Empty;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            battle.SwitchCbal(!battle.IsCbalEnabled);
            if (battle.IsCbalEnabled)
            {
                await battle.SayBattle("This battle will attempt to place clan members together when balancing.");
            }else{
                await battle.SayBattle("This battle will NOT attempt to place clan members together when balancing.");
            }
        }
    }
}
