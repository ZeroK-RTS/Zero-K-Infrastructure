using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdHelp : BattleCommand
    {
        public override string Help => "Lists other commands";
        public override string Shortcut => "help";
        public override AccessType Access => AccessType.NoCheck;

        public override BattleCommand Create() => new CmdHelp();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            return String.Empty;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            foreach (var grp in ServerBattle.Commands.Values.GroupBy(x => x.Access).OrderBy(x=>x.Key))
            {
                await battle.Respond(e, $"=== {grp.Key} {grp.Key.Description()} ===");
                foreach (var com in grp.OrderBy(x=>x.Shortcut)) await battle.Respond(e, $"!{com.Shortcut} {com.Help}");
                await battle.Respond(e, $"===");
            }
        }
    }
}