using System;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdY : BattleCommand
    {
        public override string Help => "- votes yes - in favor of current poll";
        public override string Shortcut => "y";
        public override AccessType Access => AccessType.NoCheck;

        public override BattleCommand Create() => new CmdY();
        public override string Arm(ServerBattle battle, Say e, string arguments = null) => String.Empty;
        public override async Task ExecuteArmed(ServerBattle battle, Say e) => await battle.RegisterVote(e, true);
    }
}