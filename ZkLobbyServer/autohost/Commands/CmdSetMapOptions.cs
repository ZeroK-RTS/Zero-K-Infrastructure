using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData.UnitSyncLib;

namespace ZkLobbyServer
{
    public class CmdSetMapOptions : BattleCommand
    {
        private Dictionary<string, string> options;
        private string optionsAsString;
        public override string Help => "<name>=<value>[,<name>=<value>] - applies map options";
        public override string Shortcut => "setmapoptions";
        public override AccessType Access => AccessType.NotIngameNotAutohost;

        public override BattleCommand Create() => new CmdSetMapOptions();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                battle.Respond(e, "please specify map options");
                return null;
            }

            optionsAsString = arguments;
            options = CmdSetOptions.GetParsedOptionsDictionary(battle, e, arguments, battle.HostedMapInfo?.Options);
            if (options.Count == 0) return null;
            return $"Set options {arguments} ?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.SetMapOptions(options);
            await battle.SayBattle($"map options changed {optionsAsString}");
        }

    }
}