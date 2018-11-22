using System;
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
        private Dictionary<string, bool> userVotes = new Dictionary<string, bool>();
        public bool Ended { get; private set; } = false;
        public string question { get; private set; }
        public Say Creator { get; private set; }
        private BattleCommand command;
        private bool AbsoluteMajorityVote;
        public PollOutcome Outcome { get; private set; }

        public event EventHandler<PollOutcome> PollEnded = (sender, outcome) => { };

        public CommandPoll(ServerBattle battle, bool absoluteMajorityVote = true)
        {
            this.battle = battle;
            this.AbsoluteMajorityVote = absoluteMajorityVote;
        }

        public async Task<bool> Setup(BattleCommand cmd, Say e, string args)
        {
            command = cmd.Create();

            Creator = e;
            question = command.Arm(battle, e, args);
            if (question == null) return false;
            string ignored;
            winCount = battle.Users.Values.Count(x => command.GetRunPermissions(battle, x.Name, out ignored) >= BattleCommand.RunPermission.Vote && !cmd.IsSpectator(battle, x.Name, x)) / 2 + 1;
            if (winCount <= 0) winCount = 1;

            if (!await Vote(e, true))
            {
                if (e == null) await battle.SayBattle($"Poll: {question} [!y=0/{winCount}, !n=0/{winCount}]");
            }
            else
            {
                return false;
            }

            return true;
        }

        private async Task<bool> CheckEnd(bool timeout)
        {
            var yes = userVotes.Count(x => x.Value == true);
            var no = userVotes.Count(x => x.Value == false);

            if (yes >= winCount || timeout && !AbsoluteMajorityVote && yes > no)
            {
                Ended = true;
                await battle.SayBattle($"Poll: {question} [END:SUCCESS]");
                if (command.Access == BattleCommand.AccessType.NotIngame && battle.spring.IsRunning) return true;
                if (command.Access == BattleCommand.AccessType.Ingame && !battle.spring.IsRunning) return true;
                await command.ExecuteArmed(battle, Creator);
                Outcome = new PollOutcome() { Success = true };
                return true;
            }
            else if (no >= winCount || timeout)
            {
                Ended = true;
                await battle.SayBattle($"Poll: {question} [END:FAILED]");
                Outcome = new PollOutcome() { Success = false };
                return true;
            }
            return false;
        }

        public void PublishResult()
        {
            PollEnded(this, Outcome);
        }

        public async Task End()
        {
            if (!Ended) await CheckEnd(true);
        }


        public async Task<bool> Vote(Say e, bool vote)
        {
            if (e == null) return false;
            string reason;
            if (command.GetRunPermissions(battle, e.User, out reason) >= BattleCommand.RunPermission.Vote && !Ended)
            {
                if (command.IsSpectator(battle, e.User, null)) return false;

                userVotes[e.User] = vote;

                var yes = userVotes.Count(x => x.Value == true);
                var no = userVotes.Count(x => x.Value == false);
                await battle.SayBattle(string.Format("Poll: {0} [!y={1}/{3}, !n={2}/{3}]", question, yes, no, winCount));

                if (await CheckEnd(false)) return true;
            }
            else
            {
                await battle.Respond(e, reason);
                return false;
            }
            return false;
        }
    }


    public class PollOutcome : EventArgs
    {
        public bool Success { get; set; }
    }
}
