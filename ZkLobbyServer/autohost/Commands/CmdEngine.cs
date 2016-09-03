using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaDownloader;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdEngine : BattleCommand
    {
        private string engine;
        public override string Help => "[<filters>..] - changes game engine, e.g. !engine 103.0";
        public override string Shortcut => "engine";
        public override AccessType Access => AccessType.NotIngame;

        public override BattleCommand Create() => new CmdEngine();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            engine = arguments;
            if (!ServerBattle.springPaths.HasEngineVersion(engine))
            {
                var serv = GlobalConst.GetContentService(); // TODO this can be done directly, we are in server
                if (!serv.GetEngineList(null).Any(x => x == engine))
                {
                    battle.Respond(e, "Engine not found");
                    return null;
                }
                ServerBattle.downloader.GetResource(DownloadType.ENGINE, engine);
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