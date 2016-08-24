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
            await battle.Respond(e, "---");
            foreach (var com in ServerBattle.Commands.Values.OrderBy(x=>x.Shortcut)) await battle.Respond(e, $"!{com.Shortcut} - {com.Help}");
            await battle.Respond(e, "---");
        }
    }
}