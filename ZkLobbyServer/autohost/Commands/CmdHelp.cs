using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdHelp : ServerBattleCommand
    {
        public override string Help => "Lists other commands";
        public override string Shortcut => "help";
        public override BattleCommandAccess Access => BattleCommandAccess.NoCheck;

        public override ServerBattleCommand Create() => new CmdHelp();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            return String.Empty;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            foreach (var grp in ServerBattle.Commands.Values.GroupBy(x => x.Access).OrderBy(x=>x.Key))
            {
                await battle.Respond(e, $"--- {grp.Key} ---");
                foreach (var com in grp.OrderBy(x=>x.Shortcut)) await battle.Respond(e, $"!{com.Shortcut} {com.Help}");
                await battle.Respond(e, $"--- {grp.Key} ---");
            }
            await battle.Respond(e, "---");
        }
    }
}