using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using Ratings;
using ZkData;
using static LobbyClient.BattlePoll;

namespace ZkLobbyServer
{
    public class CommandPoll
    {
        private int winCount;
        private ServerBattle battle;
        private ConcurrentDictionary<string, int> userVotes = new ConcurrentDictionary<string, int>(); //stores votes, zero indexed
        private Func<string, string> EligiblitySelector; //return null if player is allowed to vote, otherwise reason
        private readonly bool absoluteMajorityVote; //if set to yes, at least N/2 players need to vote for an option to be selected. Otherwise the option with the majority of votes wins
        private readonly int requiredWinMargin; //allow votes like resign to require more than a simple majority to pass
        private bool yesNoVote; //if set to yes, there must be only two options being yes and no.
        private bool mapSelection; //if set to yes, options have an url and represent map resources
        private bool ring;
        private string mapName;

        public bool Ended { get; private set; } = false;
        public string Topic { get; private set; }
        public List<PollOption> Options;
        public Say Creator { get; private set; }
        public PollOutcome Outcome { get; private set; }

        public event EventHandler<PollOutcome> PollEnded = (sender, outcome) => { };

        public CommandPoll(ServerBattle battle, bool yesNoVote, bool absoluteMajorityVote = true, bool mapSelection = false, string mapName = null, bool ring = false, int requiredWinMargin = 1)
        {
            this.battle = battle;
            this.absoluteMajorityVote = absoluteMajorityVote;
            this.requiredWinMargin = requiredWinMargin;
            this.yesNoVote = yesNoVote;
            this.mapSelection = mapSelection;
            this.mapName = mapName;
            this.ring = ring;
        }

        public async Task Setup(Func<string, string> eligibilitySelector, List<PollOption> options, Say creator, string Topic)
        {
            EligiblitySelector = eligibilitySelector;
            Options = options;
            Creator = creator;
            this.Topic = Topic;

            int votersCount = battle.Users.Values.Count(x => EligiblitySelector(x.Name) == null);

            winCount = votersCount / 2 + this.requiredWinMargin;
            if (winCount <= 0) {
                winCount = 1;
            } else if (winCount > votersCount)
            {
                winCount = votersCount;
            }

            await battle.server.Broadcast(battle.Users.Keys, GetBattlePoll());
            if (yesNoVote) battle.SayGame(string.Format("Poll: {0} [!y={1}/{3}, !n={2}/{3}]", Topic, userVotes.Count(x => x.Value == 0), userVotes.Count(x => x.Value == 1), winCount));

        }

        public BattlePoll GetBattlePoll()
        {
            return new BattlePoll()
            {
                Options = Options.Select((o, i) => new BattlePoll.PollOption()
                {
                    Id = i + 1,
                    Name = o.Name,
                    DisplayName = String.IsNullOrEmpty(o.DisplayName) ? o.Name : o.DisplayName,
                    Votes = userVotes.Count(x => x.Value == i),
                    Url = o.URL
                }).ToList(),
                Topic = Topic,
                VotesToWin = winCount,
                YesNoVote = yesNoVote,
                MapSelection = mapSelection,
                Url = yesNoVote ? Options[0].URL : "",
                MapName = mapName,
                NotifyPoll = ring
            };
        }

        private async Task<bool> CheckEnd(bool timeout, bool forceEnd)
        {

            List<int> votes = Options.Select((o, i) => userVotes.Count(x => x.Value == i)).ToList();
            List<int> potentialWinnerIndexes = new List<int>();
            for (int i = 0; i < votes.Count; i++) if (votes[i] == votes.Max()) potentialWinnerIndexes.Add(i);
            Random rng = new Random();
            var winnerId = potentialWinnerIndexes[rng.Next(potentialWinnerIndexes.Count)];

            if (votes[winnerId] >= winCount || timeout && !absoluteMajorityVote)
            {
                Ended = true;
                if (yesNoVote)
                {
                    if (winnerId == 0)
                    {
                        battle.SayGame($"Poll: {Topic} [END:SUCCESS]"); //Option Yes
                    }
                    else
                    {
                        battle.SayGame($"Poll: {Topic} [END:FAILED]"); //Option No
                    }
                }
                else
                {
                    battle.SayGame($"Poll: Choose {Options[winnerId].Name}? [END:SUCCESS]");
                }
                Outcome = new PollOutcome() { ChosenOption = Options[winnerId] };
                await Options[winnerId].Action();
                return true;
            }
            else if (timeout || forceEnd)
            {
                Ended = true;
                if (yesNoVote)
                {
                    battle.SayGame($"Poll: {Topic} [END:FAILED]");
                }
                else
                {
                    battle.SayGame($"Option Poll: {Topic} [END:FAILED]");
                }
                Outcome = new PollOutcome() { ChosenOption = null };
                return true;
            }
            return false;
        }

        public BattlePollOutcome GetBattlePollOutcome()
        {
            var msg = new BattlePollOutcome
            {
                Topic = Topic,
                MapSelection = mapSelection,
                YesNoVote = yesNoVote,
                WinningOption = GetBattlePoll().Options.FirstOrDefault(x => x.Name == Outcome?.ChosenOption?.Name),
            };
            msg.Success = yesNoVote ? msg.WinningOption?.Id == 1 : msg.WinningOption != null;
            msg.Message = yesNoVote ? (msg.Success ? $"Vote passed: {Topic}" : $"Vote failed: {Topic}") : (msg.Success ? $"Vote passed: Selected {msg.WinningOption.Name}." : "Vote failed: No absolute majority achieved.");
            return msg;
        }

        public async Task PublishResult()
        {
            await battle.server.Broadcast(battle.Users.Keys, GetBattlePollOutcome());
            if (mapSelection && !yesNoVote)
            {
                //store results to DB
                using (var db = new ZkDataContext())
                {
                    var cat = MapRatings.Category.Coop;
                    if (battle.Mode == PlasmaShared.AutohostMode.Teams && battle.Users.Values.Count(x => !x.IsSpectator) > 3) cat = MapRatings.Category.CasualTeams;
                    if (battle.Mode == PlasmaShared.AutohostMode.GameFFA && battle.Users.Values.Count(x => !x.IsSpectator) >= 3) cat = MapRatings.Category.FFA;
                    var outcome = new MapPollOutcome()
                    {
                        Category = cat
                    };
                    db.MapPollOutcomes.InsertOnSubmit(outcome);
                    db.SaveChanges();
                    var options = Options.Select((o, i) => new MapPollOption()
                    {
                        ResourceID = o.ResourceID,
                        Votes = userVotes.Count(x => x.Value == i),
                        MapPollID = outcome.MapPollID
                    }).ToList();
                    db.MapPollOptions.InsertAllOnSubmit(options);
                    db.SaveChanges();
                }
            }
            PollEnded(this, Outcome);
        }

        public async Task End(bool timeout)
        {
            if (!Ended) await CheckEnd(timeout, true);
        }


        //Vote for an option, one-indexed
        public async Task<bool> Vote(Say e, int vote)
        {
            if (e == null) return false;
            string ineligibilityReason = EligiblitySelector(e.User);
            if (ineligibilityReason == null && !Ended)
            {

                userVotes[e.User] = vote - 1;

                if (yesNoVote) battle.SayGame(string.Format("Poll: {0} [!y={1}/{3}, !n={2}/{3}]", Topic, userVotes.Count(x => x.Value == 0), userVotes.Count(x => x.Value == 1), winCount));
                await battle.server.Broadcast(battle.Users.Keys, GetBattlePoll());

                if (await CheckEnd(false, false)) return true;
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
        public string DisplayName;
        public Func<Task> Action;
        public string URL = "";
        public int ResourceID;
    }

    public class PollOutcome : EventArgs
    {
        public PollOption ChosenOption { get; set; }
    }
}
