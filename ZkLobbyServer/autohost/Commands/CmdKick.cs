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

            target = battle.GetAllUserNames().FirstOrDefault(x => x.Contains(arguments));
            if (target == null) target = arguments;
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