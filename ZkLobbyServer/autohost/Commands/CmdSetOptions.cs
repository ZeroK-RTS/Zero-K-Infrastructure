using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;
using ZkData.UnitSyncLib;

namespace ZkLobbyServer
{
    public class CmdSetOptions : BattleCommand
    {
        private Dictionary<string, string> options;
        private string optionsAsString;
        public override string Help => "<name>=<value>[,<name>=<value>] - applies game options";
        public override string Shortcut => "setoptions";
        public override AccessType Access => AccessType.NotIngameNotAutohost;

        public override BattleCommand Create() => new CmdSetOptions();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                battle.Respond(e, "please specify options");
                return null;
            }

            optionsAsString = arguments;
            options = GetParsedOptionsDictionary(battle, e, arguments, battle.HostedModInfo?.Options);
            if (options.Count == 0) return null;
            return $"Set options {arguments} ?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.SetModOptions(options);
            await battle.SayBattle($"options changed {optionsAsString}");
        }

        internal static Dictionary<string, string> GetParsedOptionsDictionary(ServerBattle battle, Say e, string s, IEnumerable<Option> possibleOptions)
        {
            if (possibleOptions == null || !possibleOptions.Any()) return new Dictionary<string, string>();
            var ret = new Dictionary<string, string>();

            var pairs = s.Split(new[] { ',' });
            if (pairs.Length == 0 || pairs[0].Length == 0)
            {
                battle.Respond(e, "requires key=value format");
                return ret;
            }
            foreach (var pair in pairs)
            {
                var parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    battle.Respond(e, "requires key=value format");
                    return ret;
                }
                var key = parts[0].Trim(); //Trim() to make "key = value format" ignore whitespace 
                var val = parts[1].Trim();

                var found = false;
                foreach (var o in possibleOptions)
                {
                    if (o.Key == key)
                    {
                        found = true;
                        string res;
                        if (o.GetPair(val, out res))
                        {
                            ret[key] = val;
                        }
                        else battle.Respond(e, "Value " + val + " is not valid for this option");

                        break;
                    }
                }
                if (!found)
                {
                    battle.Respond(e, "No option called " + key + " found");
                    return ret;
                }
            }
            return ret;
        }
    }
}