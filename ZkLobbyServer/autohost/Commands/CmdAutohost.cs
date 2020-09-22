using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaDownloader;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdAutohost : BattleCommand
    {
        private string engine;
        public override string Help => "changes this host into an autohost or sets you as founder";
        public override string Shortcut => "autohost";
        public override AccessType Access => AccessType.Admin;

        public override BattleCommand Create() => new CmdAutohost();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (battle.IsMatchMakerBattle || battle.ApplicableRating != RatingCategory.Casual)
            {
                battle.Respond(e, "Only casual battles be repurposed as autohosts");
                return null;
            }
            return string.Empty;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            battle.SwitchAutohost(!battle.IsAutohost, e?.User);
            if (battle.IsAutohost){
                await battle.SayBattle("This battle is now an autohost. It will stay open until this command is executed again. Maps are rotated after each game. Title, player limit, password and modoptions are locked. Use !maxelo, !minelo, !maxlevel, !minlevel, !minmapsupportlevel to customize this autohost.");
            }else{
                await battle.SayBattle("This battle is no longer an autohost, it will close as soon as it is empty.");
            }
        }
    }
}
