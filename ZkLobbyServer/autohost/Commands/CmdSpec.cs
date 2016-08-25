using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdSpec : BattleCommand
    {
        public override string Help => "[<filters>..] - makes player a spectator. When player not specified, spectates AFK players";
        public override string Shortcut => "spec";
        public override AccessType Access => AccessType.NotIngame;

        public override BattleCommand Create() => new CmdSpec();

        private string target;

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                target = null;
                return $"do you want to spectate AFK?";
            }

            int[] indexes;
            string[] usrlist;
            var words = arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (battle.FilterUsers(words, out usrlist, out indexes) == 0)
            {
                battle.Respond(e, "Player not found");
                return null;
            }
            else target = usrlist[0];
            return $"do you want to spectate {target}?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (target == null)
            {
                foreach (var usr in battle.Users.Values.Where(x => !x.IsSpectator && x.LobbyUser.IsAway)) await battle.Spectate(usr.Name);
            }
            else
            {
                await battle.Spectate(target);
            }

            await battle.SayBattle($"forcing {target ?? "AFK"} to spectator");
        }
    }
}