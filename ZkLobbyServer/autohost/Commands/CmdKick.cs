using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdKick : BattleCommand
    {
        public override string Help => "[<filters>..] - kicks a player";
        public override string Shortcut => "kick";
        public override AccessType Access => AccessType.Anywhere;

        public override BattleCommand Create() => new CmdKick();

        private string target;

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                battle.Respond(e, "You must specify a player name");
                return null;
            }

            int[] indexes;
            string[] usrlist;
            var words = arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (battle.FilterUsers(words, out usrlist, out indexes) == 0) target = arguments;
            else target = usrlist[0];
            return $"do you want to kick {target}?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (!battle.kickedPlayers.Any(x => x.Name == target)) battle.kickedPlayers.Add(new ServerBattle.KickedPlayer() { Name = target });
            if (battle.spring.IsRunning) battle.spring.Kick(target);
            await battle.KickFromBattle(target, $"by {e?.User}");
        }
    }
}