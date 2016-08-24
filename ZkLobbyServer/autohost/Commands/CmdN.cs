using System;
using System.Threading.Tasks;
using LobbyClient;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdN : ServerBattleCommand
    {
        public override string Help => "- votes no - against current poll";
        public override string Shortcut => "n";
        public override BattleCommandAccess Access => BattleCommandAccess.NoCheck;

        public override ServerBattleCommand Create() => new CmdN();
        public override string Arm(ServerBattle battle, Say e, string arguments = null) => String.Empty;
        public override async Task ExecuteArmed(ServerBattle battle, Say e) => await battle.RegisterVote(e, false);
    }
}