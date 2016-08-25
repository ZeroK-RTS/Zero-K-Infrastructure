using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdSetOptions : BattleCommand
    {
        private Dictionary<string, string> options;
        private string optionsAsString;
        public override string Help => "<name>=<value>[,<name>=<value>] - applies game/map options";
        public override string Shortcut => "setoptions";
        public override AccessType Access => AccessType.NotIngame;

        public override BattleCommand Create() => new CmdSetOptions();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                battle.Respond(e, "please specify options");
                return null;
            }

            optionsAsString = arguments;
            options = battle.GetOptionsDictionary(e, arguments);
            if (options.Count == 0) return null;
            return $"Set options {arguments} ?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.SetModOptions(options);
            await battle.SayBattle($"options changed {optionsAsString}");
        }
    }
}