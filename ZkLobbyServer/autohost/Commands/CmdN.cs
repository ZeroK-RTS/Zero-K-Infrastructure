using System;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdN : BattleCommand
    {
        public override string Help => "- votes no - against current poll";
        public override string Shortcut => "n";
        public override AccessType Access => AccessType.NoCheck;

        public override BattleCommand Create() => new CmdN();
        public override string Arm(ServerBattle battle, Say e, string arguments = null) => String.Empty;
        public override async Task ExecuteArmed(ServerBattle battle, Say e) => await battle.RegisterVote(e, 2);
    }
}