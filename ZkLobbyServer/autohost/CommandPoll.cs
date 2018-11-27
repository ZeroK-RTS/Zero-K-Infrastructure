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
        private Dictionary<string, int> userVotes = new Dictionary<string, int>(); //stores votes, zero indexed
        private Func<string, string> EligiblitySelector; //return null if player is allowed to vote, otherwise reason
        private bool AbsoluteMajorityVote; //if set to yes, at least N/2 players need to vote for an option to be selected. Otherwise the option with the majority of votes wins
        private bool DefaultPoll; //if set to yes, there must be only two options being yes and no.

        public bool Ended { get; private set; } = false;
        public string Topic { get; private set; }
        public List<PollOption> Options;
        public Say Creator { get; private set; }
        public PollOutcome Outcome { get; private set; }

        public event EventHandler<PollOutcome> PollEnded;

        public CommandPoll(ServerBattle battle, bool absoluteMajorityVote = true, bool defaultPoll = false)
        {
            this.battle = battle;
            this.AbsoluteMajorityVote = absoluteMajorityVote;
            DefaultPoll = defaultPoll;
        }

        public async Task Setup(Func<string, string> eligibilitySelector, List<PollOption> options, Say creator, string Topic)
        {
            EligiblitySelector = eligibilitySelector;
            Options = options;
            Creator = creator;


            winCount = battle.Users.Values.Count(x => EligiblitySelector(x.Name) == null) / 2 + 1;
            if (winCount <= 0) winCount = 1;

            await battle.server.Broadcast(battle.Users.Keys, GetBattlePoll());
        }

        public BattlePoll GetBattlePoll()
        {
            return new BattlePoll()
            {
                Options = Options.Select((o, i) => new BattlePoll.PollOption()
                {
                    Id = i + 1,
                    Name = o.Name,
                    Votes = userVotes.Count(x => x.Value == i)
                }).ToList(),
                Topic = Topic,
                VotesToWin = winCount,
                DefaultPoll = DefaultPoll
            };
        }

        private async Task<bool> CheckEnd(bool timeout)
        {

            List<int> votes = Options.Select((o, i) => userVotes.Count(x => x.Value == i)).ToList();
            var winnerId = votes.IndexOf(votes.Max());

            if (votes[winnerId] >= winCount || timeout && !AbsoluteMajorityVote)
            {
                Ended = true;
                if (DefaultPoll)
                {
                    await battle.SayBattle($"Poll: {Topic} [END:SUCCESS]");
                }
                else
                {
                    await battle.SayBattle($"Option Poll: {Topic} [END: Selected {Options[winnerId].Name}]");
                }
                await Options[winnerId].Action();
                Outcome = new PollOutcome() { ChosenOption = Options[winnerId] };
                return true;
            }
            else if (timeout)
            {
                Ended = true;
                if (DefaultPoll)
                {
                    await battle.SayBattle($"Poll: {Topic} [END:FAILED]");
                }
                else
                {
                    await battle.SayBattle($"Option Poll: {Topic} [END: No option achieved absolute majority]");
                }
                Outcome = new PollOutcome() { ChosenOption = null };
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


        //Vote for an option, one-indexed
        public async Task<bool> Vote(Say e, int vote)
        {
            if (e == null) return false;
            string ineligibilityReason = EligiblitySelector(e.User);
            if (ineligibilityReason == null && !Ended)
            {

                userVotes[e.User] = vote - 1;

                if (DefaultPoll) await battle.SayBattle(string.Format("Poll: {0} [!y={1}/{3}, !n={2}/{3}]", Topic, userVotes.Count(x => x.Value == 0), userVotes.Count(x => x.Value == 1), winCount));
                await battle.server.Broadcast(battle.Users.Keys, GetBattlePoll());

                if (await CheckEnd(false)) return true;
            }
            else
            {
                await battle.Respond(e, ineligibilityReason);
                return false;
            }
            return false;
        }

    }
    public class PollOption
    {
        public string Name;
        public Func<Task> Action;
    }

    public class PollOutcome : EventArgs
    {
        public PollOption ChosenOption { get; set; }
    }
}
