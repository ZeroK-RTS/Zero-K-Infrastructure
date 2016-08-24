using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CommandPoll
    {
        private int winCount;
        private ServerBattle battle;
        private bool ended;
        private Dictionary<string, bool> userVotes = new Dictionary<string, bool>();
        private string question;
        private Say creator;
        private ServerBattleCommand command;

        public CommandPoll(ServerBattle battle)
        {
            this.battle = battle;
        }

        public async Task<bool> Setup(ServerBattleCommand cmd, Say e, string args)
        {
            command = cmd.Create();

            creator = e;
            question = command.Arm(battle, e, args);
            if (question == null) return false;
            winCount = battle.Users.Values.Count(x => command.RunPermissions(battle, x.Name) >= CommandExecutionRight.Vote) / 2 + 1;
            if (winCount <= 0) winCount = 1;

            if (winCount <= 0) winCount = (battle.NonSpectatorCount / 2 + 1);
            if (winCount <= 0) winCount = 1;

            if (!await Vote(e, true)) await battle.SayBattle($"Poll: {question} [!y=0/{winCount}, !n=0/{winCount}]");
            return true;
        }


        public async Task End()
        {
            if (!ended) await battle.SayBattle($"Poll: {question} [END:FAILED]");
            ended = true;
        }


        public async Task<bool> Vote(Say e, bool vote)
        {
            if (command.RunPermissions(battle, e.User) >= CommandExecutionRight.Vote)
            {
                userVotes[e.User] = vote;
                var yes = userVotes.Count(x => x.Value == true);
                var no = userVotes.Count(x => x.Value == false);
                await battle.SayBattle(string.Format("Poll: {0} [!y={1}/{3}, !n={2}/{3}]", question, yes, no, winCount));
                if (yes >= winCount)
                {
                    await battle.SayBattle($"Poll: {question} [END:SUCCESS]");
                    ended = true;
                    await command.ExecuteArmed(battle, creator);
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