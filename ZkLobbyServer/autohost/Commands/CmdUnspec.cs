using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdUnspec: BattleCommand
    {
        private string target;
        public override AccessType Access => AccessType.NotIngame;
        public override string Help => "[<filters>..] - makes a spectator a player. When player not specified, unspectates non-AFK players";
        public override string Shortcut => "unspec";

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                target = null;
                return $"do you want to unspectate non-AFK?";
            }

            target = battle.GetAllUserNames().FirstOrDefault(x => x.Contains(arguments));
            if (target == null)
            {
                battle.Respond(e, "Player not found");
                return null;
            }
            return $"do you want to unspectate {target}?";
        }

        public override BattleCommand Create() => new CmdUnspec();


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (target == null)
            {
                foreach (var usr in battle.Users.Values.Where(x => x.IsSpectator && !x.LobbyUser.IsAway)) await battle.Unspectate(usr.Name);
            }
            else
            {
                await battle.Unspectate(target);
            }

            await battle.SayBattle($"forcing {target ?? "non-AFK"} to player");
        }
    }
}