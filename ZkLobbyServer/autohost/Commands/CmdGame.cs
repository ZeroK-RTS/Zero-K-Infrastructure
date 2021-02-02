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
    public class CmdGame : BattleCommand
    {
        private Resource game;
        public override string Help => "[<filters>..] - changes game version, e.g. !game zk:test. Use zk:dev to host local dev version";
        public override string Shortcut => "game";
        public override AccessType Access => AccessType.NotIngameNotAutohost;

        public override BattleCommand Create() => new CmdGame();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (string.IsNullOrEmpty(arguments)) arguments = battle.server.Game ?? GlobalConst.DefaultZkTag;
            game = MapPicker.FindResources(ResourceType.Mod, arguments).FirstOrDefault();


            if (game == null)
            {
                battle.Respond(e, "Cannot find such game.");
                return null;
            }

            return $"Change game to {game.RapidTag} {game.InternalName}?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (game != null)
            {
                await battle.SwitchGame(game.InternalName);
                await battle.SayBattle("Changing game to " + game.InternalName);
                battle.SwitchDefaultGame(false);
                await battle.SayBattle("This host will no longer update its game automatically");
            }
            
        }
    }
}
