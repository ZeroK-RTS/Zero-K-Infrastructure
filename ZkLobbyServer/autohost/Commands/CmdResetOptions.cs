using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyClient;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdResetOptions : ServerBattleCommand
    {
        public override string Help => "sets default game/map options";
        public override string Shortcut => "resetoptions";
        public override BattleCommandAccess Access => BattleCommandAccess.NotIngame;

        public override ServerBattleCommand Create() => new CmdResetOptions();

        public override string Arm(ServerBattle battle, Say e, string arguments = null) => string.Empty;


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.SetModOptions(new Dictionary<string, string>());
            await battle.SayBattle($"options reset to defaults");
        }
    }
}