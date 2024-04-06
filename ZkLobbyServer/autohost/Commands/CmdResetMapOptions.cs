using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdResetMapOptions : BattleCommand
    {
        public override string Help => "sets default map options";
        public override string Shortcut => "resetmapoptions";
        public override AccessType Access => AccessType.NotIngameNotAutohost;

        public override BattleCommand Create() => new CmdResetMapOptions();

        public override string Arm(ServerBattle battle, Say e, string arguments = null) => "Reset map options to default?";


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.SetMapOptions(new Dictionary<string, string>());
            await battle.SayBattle($"map options reset to defaults");
        }
    }
}