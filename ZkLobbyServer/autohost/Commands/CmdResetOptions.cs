using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdResetOptions : BattleCommand
    {
        public override string Help => "sets default game options";
        public override string Shortcut => "resetoptions";
        public override AccessType Access => AccessType.NotIngameNotAutohost;

        public override BattleCommand Create() => new CmdResetOptions();

        public override string Arm(ServerBattle battle, Say e, string arguments = null) => "Reset game options to default?";


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.SetModOptions(new Dictionary<string, string>());
            await battle.SayBattle($"options reset to defaults");
        }
    }
}