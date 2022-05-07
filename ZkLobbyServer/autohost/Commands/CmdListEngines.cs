using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdListEngines : BattleCommand
    {
        private List<string> engines;
        public override string Help => "[<filters>..] - lists game engines, e.g. !listengines 103.0";
        public override string Shortcut => "listengines";
        public override AccessType Access => AccessType.NoCheck;

        public override BattleCommand Create() => new CmdListEngines();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            var serv = GlobalConst.GetContentService(); // TODO this can be done directly, we are in server
            engines = serv.Query(new GetEngineListRequest()).Engines;
            if (!string.IsNullOrEmpty(arguments)) engines = engines.Where(x => x.Contains(arguments)).ToList();
            return String.Empty;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (engines.Any())
            {
                await battle.Respond(e, "---");
                foreach (var eng in engines) await battle.Respond(e, eng);
                await battle.Respond(e, "---");
            }
            else await battle.Respond(e, "no such engine found");
        }
    }
}