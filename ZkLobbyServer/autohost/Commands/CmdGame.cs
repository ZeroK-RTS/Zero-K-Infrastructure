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
    public class CmdGame : ServerBattleCommand
    {
        private Resource game;
        public override string Help => "[<filters>..] - changes serverg game, eg. !game zk:test. Use zk:dev to host local dev version";
        public override string Shortcut => "game";
        public override BattleCommandAccess Access => BattleCommandAccess.NotIngame;

        public override ServerBattleCommand Create() => new CmdGame();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (string.IsNullOrEmpty(arguments)) arguments = "zk:stable";
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
                await battle.SwitchMap(game.InternalName);
                await battle.SayBattle("changing game to " + game.InternalName);
            }
            
        }
    }
}