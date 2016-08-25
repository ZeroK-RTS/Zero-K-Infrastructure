using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdListGames : BattleCommand
    {
        private List<Resource> games;
        public override string Help => "[<filters>..] - lists games on server, e.g. !listgames Zero-K";
        public override string Shortcut => "listgames";
        public override AccessType Access => AccessType.NoCheck;

        public override BattleCommand Create() => new CmdListGames();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            games = MapPicker.FindResources(ResourceType.Mod, arguments).Take(200).ToList();
            return String.Empty;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (games.Any())
            {
                await battle.Respond(e, "---");
                foreach (var map in games) await battle.Respond(e, map.ResourceID + ": " + map.InternalName);
                await battle.Respond(e, "---");
            }
            else await battle.Respond(e, "no such game found");
        }
    }
}