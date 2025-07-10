using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaDownloader;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdEngine : BattleCommand
    {
        private string engine;
        public override string Help => "[<filters>..] - changes game engine, e.g. !engine 103.0";
        public override string Shortcut => "engine";
        public override AccessType Access => AccessType.NotIngameNotAutohost;

        public override BattleCommand Create() => new CmdEngine();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            engine = string.IsNullOrEmpty(arguments) ? battle.server.Engine : arguments;

            if ((battle.Mode != AutohostMode.None || !battle.IsPassworded) && engine != battle.server.Engine && !battle.IsAutohost)
            {
                battle.Respond(e, $"You cannot change engine to version other than {battle.server.Engine} here, must be the Custom room type (say '!type custom') and passworded (say '!password bla', can remove it afterwards via '!password')");
                return null;
            }

            if (!battle.server.SpringPaths.HasEngineVersion(engine))
            {
                var serv = GlobalConst.GetContentService(); // TODO this can be done directly, we are in server
                if (!serv.Query(new GetEngineListRequest()).Engines.Any(x => x == engine))
                {
                    battle.Respond(e, "Engine not found");
                    return null;
                }
                battle.server.Downloader.GetResource(DownloadType.ENGINE, engine);
            }

            return $"Change engine to {engine}?";
           
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (engine != null)
            {
                await battle.SwitchEngine(engine);
                await battle.SayBattle("Engine changed to " + engine);
            }

        }
    }
}
