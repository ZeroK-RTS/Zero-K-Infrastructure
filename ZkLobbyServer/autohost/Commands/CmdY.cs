using System;
using System.Threading.Tasks;
using LobbyClient;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdY : ServerBattleCommand
    {
        public override string Help => "- votes yes - in favor of current poll";
        public override string Shortcut => "y";
        public override BattleCommandAccess Access => BattleCommandAccess.NoCheck;

        public override ServerBattleCommand Create() => new CmdY();
        public override string Arm(ServerBattle battle, Say e, string arguments = null) => String.Empty;
        public override async Task ExecuteArmed(ServerBattle battle, Say e) => await battle.RegisterVote(e, true);
    }
}