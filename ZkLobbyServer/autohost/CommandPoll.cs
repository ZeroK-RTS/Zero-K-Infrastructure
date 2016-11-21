using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CommandPoll
    {
        private int winCount;
        private ServerBattle battle;
        private bool ended;
        private Dictionary<string, bool> userVotes = new Dictionary<string, bool>();
        private string question;
        public Say Creator { get; private set; }
        private BattleCommand command;

        public CommandPoll(ServerBattle battle)
        {
            this.battle = battle;
        }

        public async Task<bool> Setup(BattleCommand cmd, Say e, string args)
        {
            command = cmd.Create();

            Creator = e;
            question = command.Arm(battle, e, args);
            if (question == null) return false;
            winCount = battle.Users.Values.Count(x => command.GetRunPermissions(battle, x.Name) >= BattleCommand.RunPermission.Vote && !cmd.IsSpectator(battle, x.Name, x)) / 2 + 1;
            if (winCount <= 0) winCount = 1;

            if (winCount <= 0) winCount = (battle.NonSpectatorCount / 2 + 1);
            if (winCount <= 0) winCount = 1;

            if (!await Vote(e, true)) await battle.SayBattle($"Poll: {question} [!y=0/{winCount}, !n=0/{winCount}]");
            else return false;

            return true;
        }


        public async Task End()
        {
            if (!ended) await battle.SayBattle($"Poll: {question} [END:FAILED]");
            ended = true;
        }


        public async Task<bool> Vote(Say e, bool vote)
        {
            if (command.GetRunPermissions(battle, e.User) >= BattleCommand.RunPermission.Vote && !ended)
            {
                if (command.IsSpectator(battle, e.User, null)) return false;

                userVotes[e.User] = vote;
                var yes = userVotes.Count(x => x.Value == true);
                var no = userVotes.Count(x => x.Value == false);

                if (yes >= winCount) ended = true;

                await battle.SayBattle(string.Format("Poll: {0} [!y={1}/{3}, !n={2}/{3}]", question, yes, no, winCount));

                if (yes >= winCount)
                {
                    ended = true;
                    await battle.SayBattle($"Poll: {question} [END:SUCCESS]");
                    if (command.Access == BattleCommand.AccessType.NotIngame && battle.spring.IsRunning) return true;
                    if (command.Access == BattleCommand.AccessType.Ingame && !battle.spring.IsRunning) return true;
                    await command.ExecuteArmed(battle, Creator);
                    return true;
                }
                else if (no >= winCount)
                {
                    await End();
                    return true;
                }
            }
            else
            {
                await battle.Respond(e, "You are not allowed to vote");
                return false;
            }
            return false;
        }
    };
}