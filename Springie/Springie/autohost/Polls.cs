#region using

using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using PlasmaShared.SpringieInterfaceReference;

#endregion

namespace Springie.autohost
{
    public class VoteResign: AbstractPoll, IVotable
    {
        BattleContext context;
        readonly Dictionary<string, int> userVotes = new Dictionary<string, int>();
        PlayerTeam voteStarter;
        int winCount = 0;
        public VoteResign(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}


        public bool Init(TasSayEventArgs e, string[] words)
        {
            if (spring.IsRunning)
            {
                context = spring.StartContext;
                voteStarter = context.Players.FirstOrDefault(x => x.Name == e.UserName && !x.IsSpectator);
                if (voteStarter != null)
                {
                    ah.SayBattle(string.Format("Do you want to resign team {0}? !vote 1 = yes, !vote 2 = no", voteStarter.AllyID + 1));
                    winCount = context.Players.Count(x => x.AllyID == voteStarter.AllyID && !x.IsSpectator)/2 + 1;
                    return true;
                }
            }
            AutoHost.Respond(tas, spring, e, "You cannot resign now");
            return false;
        }

        public bool Vote(TasSayEventArgs e, string[] words)
        {
            if (spring.IsRunning)
            {
                var entry = spring.StartContext.Players.FirstOrDefault(x => x.Name == e.UserName);
                if (entry == null || entry.IsSpectator || entry.AllyID != voteStarter.AllyID) ah.Respond(e, string.Format("Only team {0} can vote", voteStarter.AllyID + 1));
                else
                {
                    int num;
                    int.TryParse(words[0], out num);
                    if (num == 1 || num == 2)
                    {
                        userVotes[e.UserName] = num;
                        ah.SayBattle(string.Format("option {0} has {1} of {2} votes", num, userVotes.Count(x => x.Value == num), winCount));
                    }
                }
            }

            var yesCnt = userVotes.Count(x => x.Value == 1);
            var noCnt = userVotes.Count(x => x.Value == 2);
            var success = yesCnt > noCnt && (yesCnt >= winCount || yesCnt > 1);
            var endPoll = hackEndTimeVote || yesCnt >= winCount || noCnt >= winCount;
            if (endPoll)
            {
                if (success)
                {
                    ah.SayBattle("vote successful - resigning");
                    if (spring.IsRunning) foreach (var p in context.Players.Where(x => x.AllyID == voteStarter.AllyID && !x.IsSpectator)) spring.ResignPlayer(p.Name);
                }
                else ah.SayBattle("not enough votes");
                return true;
            }
            else return false;
        }
    }

    public interface IVotable
    {
        bool Init(TasSayEventArgs e, string[] words);
        void TimeEnd();
        bool Vote(TasSayEventArgs e, string[] words);
    }

    public abstract class AbstractPoll
    {
        protected AutoHost ah;
        protected int defaultWinVote = 1;
        protected bool hackEndTimeVote;
        protected int initialUserCount;
        protected int lastVote = -1; // last registered vote value
        protected int options = 2;
        protected Spring spring;
        protected TasClient tas;

        protected List<string> users = new List<string>();
        protected List<int> votes = new List<int>();
        public virtual double Ratio { get { return 0.5; } }

        public AbstractPoll() {}

        public AbstractPoll(TasClient tas, Spring spring, AutoHost ah)
        {
            this.tas = tas;
            this.spring = spring;
            this.ah = ah;

            initialUserCount = 0;
            var b = tas.MyBattle;
            if (b != null)
            {
                foreach (var us in b.Users)
                {
                    if (us.Name != tas.UserName)
                    {
                        users.Add(us.Name);
                        votes.Add(0);
                        if (!us.IsSpectator) initialUserCount++;
                    }
                }
            }
        }

        public virtual void TimeEnd()
        {
            hackEndTimeVote = true;
            var iv = this as IVotable;
            if (iv != null) iv.Vote(TasSayEventArgs.Default, new string[] { });
        }

        protected bool CheckEnd(out int winVote)
        {
            var sums = new int[options];
            foreach (var val in votes) if (val > 0 && val <= options) sums[val - 1]++;

            var votesLeft = votes.FindAll(delegate(int t) { return (t == 0); }).Count;
            var canDecide = false;
            var winLimit = (int)(initialUserCount*Ratio);

            var max = 0;
            var maxCount = 0;
            for (var i = 0; i < sums.Length; ++i) if (sums[i] > max) max = sums[i];
            for (var i = 0; i < sums.Length; ++i) if (sums[i] == max) maxCount++;

            for (var i = 0; i < sums.Length; ++i)
            {
                var text = string.Format("option {0} has {1} of {2} votes", i + 1, sums[i], winLimit + 1);

                if (!hackEndTimeVote && i + 1 == lastVote) ah.SayBattle(text);

                if (sums[i] > winLimit)
                {
                    winVote = i + 1;
                    return true;
                }
                if (hackEndTimeVote && sums[i] >= 2 && sums[i] == max && maxCount == 1)
                {
                    winVote = i + 1;
                    return true;
                }

                if (sums[i] + votesLeft > winLimit) canDecide = true;
            }
            winVote = 0;
            if (!canDecide) return true;
            else return false;
        }

        protected bool RegisterVote(TasSayEventArgs e, string[] words, out int vote)
        {
            vote = 0;
            if (hackEndTimeVote) return true;
            if (words.Length != 1) return false;
            int.TryParse(words[0], out vote);
            if (vote > 0 && vote <= options)
            {
                // vote within parameters, lets register it
                lastVote = vote;

                var ind = users.IndexOf(e.UserName);
                var b = tas.MyBattle;
                if (b != null)
                {
                    var bidx = b.GetUserIndex(e.UserName);
                    if (bidx > -1) if (b.Users[bidx].IsSpectator) return false;
                    if (ind == -1)
                    {
                        votes.Add(vote);
                        users.Add(e.UserName);
                    }
                    else votes[ind] = vote;
                    return true;
                }
            }
            return false;
        }
    };

    public class VoteMap: AbstractPoll, IVotable
    {
        string map;

        public VoteMap(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        bool IVotable.Init(TasSayEventArgs e, string[] words)
        {
            if (words.Length > 0)
            {
                string[] vals;
                int[] indexes;
                if (ah.FilterMaps(words, out vals, out indexes) > 0)
                {
                    map = vals[0];
                    ah.SayBattle("Do you want to change map to " + map + "? !vote 1 = yes, !vote 2 = no");
                    return true;
                }
                else
                {
                    AutoHost.Respond(tas, spring, e, "Cannot find such map");
                    return false;
                }
            }
            else
            {
                ah.SayBattle("Do you want to change to suitable random map? !vote 1 = yes, !vote 2 = no");
                return true;
            }
        }

        public bool Vote(TasSayEventArgs e, string[] words)
        {
            int vote;
            if (!RegisterVote(e, words, out vote))
            {
                AutoHost.Respond(tas, spring, e, "You must vote valid option/not be a spectator");
                return false;
            }

            int winVote;
            if (CheckEnd(out winVote))
            {
                if (winVote == 1)
                {
                    if (string.IsNullOrEmpty(map))
                    {
                        ah.SayBattle("vote successful - changing to a suitable random map");
                        ah.ComMap(TasSayEventArgs.Default, new string[] { });
                    }
                    else
                    {
                        ah.SayBattle("vote successful - changing map to " + map);
                        ah.ComMap(TasSayEventArgs.Default, new string[] { map });
                    }
                }
                else ah.SayBattle("not enough votes, map stays");
                return true;
            }
            else return false;
        }
    }

    public class VoteKick: AbstractPoll, IVotable
    {
        string player;
        public override double Ratio { get { return 0.66; } }

        public VoteKick(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        public bool Init(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                AutoHost.Respond(tas, spring, e, "You must specify player name");
                return false;
            }

            string[] players;
            int[] indexes;
            if (AutoHost.FilterUsers(words, tas, spring, out players, out indexes) > 0)
            {
                player = players[0];
                ah.SayBattle("Do you want to kick " + player + "? !vote 1 = yes, !vote 2 = no");
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "Cannot find such player");
                return false;
            }
        }

        public bool Vote(TasSayEventArgs e, string[] words)
        {
            int vote;
            if (!RegisterVote(e, words, out vote))
            {
                AutoHost.Respond(tas, spring, e, "You must vote valid option/not be a spectator");
                return false;
            }

            int winVote;
            if (CheckEnd(out winVote))
            {
                if (winVote == 1)
                {
                    ah.SayBattle("vote successful - kicking " + player);
                    ah.ComKick(TasSayEventArgs.Default, new[] { player });
                }
                else ah.SayBattle("not enough votes, player stays");
                return true;
            }
            else return false;
        }
    }


    public class VoteSpec: AbstractPoll, IVotable
    {
        string player;

        public VoteSpec(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        public bool Init(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                AutoHost.Respond(tas, spring, e, "You must specify player name");
                return false;
            }

            string[] players;
            int[] indexes;
            if (AutoHost.FilterUsers(words, tas, spring, out players, out indexes) > 0)
            {
                player = players[0];
                ah.SayBattle("Do you want to spectate " + player + "? !vote 1 = yes, !vote 2 = no");
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "Cannot find such player");
                return false;
            }
        }

        public bool Vote(TasSayEventArgs e, string[] words)
        {
            int vote;
            if (!RegisterVote(e, words, out vote))
            {
                AutoHost.Respond(tas, spring, e, "You must vote valid option/not be a spectator");
                return false;
            }

            int winVote;
            if (CheckEnd(out winVote))
            {
                if (winVote == 1)
                {
                    ah.SayBattle("vote successful - speccing " + player);
                    ah.ComForceSpectator(TasSayEventArgs.Default, new[] { player });
                }
                else ah.SayBattle("not enough votes, player stays");
                return true;
            }
            else return false;
        }
    }


    public class VoteSplitPlayers: AbstractPoll, IVotable
    {
        public VoteSplitPlayers(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        public bool Init(TasSayEventArgs e, string[] words)
        {
            ah.SayBattle("Do you want to split game into two? !vote 1 = yes, !vote 2 = no");
            return true;
        }

        public bool Vote(TasSayEventArgs e, string[] words)
        {
            int vote;
            if (!RegisterVote(e, words, out vote))
            {
                AutoHost.Respond(tas, spring, e, "You must vote valid option/not be a spectator");
                return false;
            }

            int winVote;
            if (CheckEnd(out winVote))
            {
                if (winVote == 1)
                {
                    ah.SayBattle("vote successful - splitting ");
                    ah.ComSplitPlayers(TasSayEventArgs.Default, new string[] { });
                }
                else ah.SayBattle("not enough votes");
                return true;
            }
            else return false;
        }
    }

    public class VoteForce: AbstractPoll, IVotable
    {
        public VoteForce(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        public bool Init(TasSayEventArgs e, string[] words)
        {
            if (spring.IsRunning)
            {
                ah.SayBattle("Do you want to force game? !vote 1 = yes, !vote 2 = no");
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "battle not started yet");
                return false;
            }
        }


        public bool Vote(TasSayEventArgs e, string[] words)
        {
            int vote;
            if (!RegisterVote(e, words, out vote))
            {
                AutoHost.Respond(tas, spring, e, "You must vote valid option/not be a spectator");
                return false;
            }

            int winVote;
            if (CheckEnd(out winVote))
            {
                if (winVote == 1)
                {
                    ah.ComForce(e, words);
                    ah.SayBattle("vote successful - forcing");
                }
                else ah.SayBattle("not enough votes");
                return true;
            }
            else return false;
        }
    }

    public class VoteForceStart: AbstractPoll, IVotable
    {
        public VoteForceStart(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        public bool Init(TasSayEventArgs e, string[] words)
        {
            if (!spring.IsRunning)
            {
                ah.SayBattle("Do you want to force start game? !vote 1 = yes, !vote 2 = no");
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "battle already started");
                return false;
            }
        }


        public bool Vote(TasSayEventArgs e, string[] words)
        {
            int vote;
            if (!RegisterVote(e, words, out vote))
            {
                AutoHost.Respond(tas, spring, e, "You must vote valid option/not be a spectator");
                return false;
            }

            int winVote;
            if (CheckEnd(out winVote))
            {
                if (winVote == 1)
                {
                    ah.SayBattle("vote successful - force starting");
                    ah.ComForceStart(e, words);
                }
                else ah.SayBattle("not enough votes");
                return true;
            }
            else return false;
        }
    }

    public class VoteExit: AbstractPoll, IVotable
    {
        public override double Ratio { get { return 0.66; } }
        public VoteExit(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        public bool Init(TasSayEventArgs e, string[] words)
        {
            if (spring.IsRunning)
            {
                ah.SayBattle("Do you want to exit this game? !vote 1 = yes, !vote 2 = no");
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "game not running");
                return false;
            }
        }

        public bool Vote(TasSayEventArgs e, string[] words)
        {
            int vote;
            if (!RegisterVote(e, words, out vote))
            {
                AutoHost.Respond(tas, spring, e, "You must vote valid option/not be a spectator");
                return false;
            }

            int winVote;
            if (CheckEnd(out winVote))
            {
                if (winVote == 1)
                {
                    ah.SayBattle("vote successful - force exiting");
                    ah.ComExit(e, words);
                }
                else ah.SayBattle("not enough votes");
                return true;
            }
            else return false;
        }
    }

    public class VoteRehost: AbstractPoll, IVotable
    {
        new const double ratio = 0.66;

        string modname = "";

        public VoteRehost(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        public bool Init(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                ah.SayBattle("Do you want to rehost this game? !vote 1 = yes, !vote 2 = no");
                return true;
            }
            else
            {
                string[] mods;
                int[] indexes;
                if (AutoHost.FilterMods(words, ah, out mods, out indexes) == 0)
                {
                    AutoHost.Respond(tas, spring, e, "cannot find such mod");
                    return false;
                }
                else
                {
                    modname = mods[0];
                    ah.SayBattle("Do you want to rehost this game to " + modname + "? !vote 1 = yes, !vote 2 = no");
                    return true;
                }
            }
        }

        public bool Vote(TasSayEventArgs e, string[] words)
        {
            int vote;
            if (!RegisterVote(e, words, out vote))
            {
                AutoHost.Respond(tas, spring, e, "You must vote valid option/not be a spectator");
                return false;
            }

            int winVote;
            if (CheckEnd(out winVote))
            {
                if (winVote == 1)
                {
                    ah.SayBattle("vote successful - rehosting");

                    ah.ComRehost(e, new[] { modname });
                }
                else ah.SayBattle("not enough votes");
                return true;
            }
            else return false;
        }
    }


    public class VoteBoss: AbstractPoll, IVotable
    {
        string player;

        //new const double ratio = 0.50;

        public VoteBoss(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        public bool Init(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                if (ah.BossName == "")
                {
                    ah.Respond(e, "there is currently no boss to remove");
                    return false;
                }
                else
                {
                    player = "";
                    ah.SayBattle("Do you want to remove current boss " + ah.BossName + "? !vote 1 = yes, !vote 2 = no");
                    return true;
                }
            }

            string[] players;
            int[] indexes;
            if (AutoHost.FilterUsers(words, tas, spring, out players, out indexes) > 0)
            {
                player = players[0];
                ah.SayBattle("Do you want to elect " + player + " for the boss? !vote 1 = yes, !vote 2 = no");
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "Cannot find such player");
                return false;
            }
        }

        public bool Vote(TasSayEventArgs e, string[] words)
        {
            int vote;
            if (!RegisterVote(e, words, out vote))
            {
                AutoHost.Respond(tas, spring, e, "You must vote valid option/not be a spectator");
                return false;
            }

            int winVote;
            if (CheckEnd(out winVote))
            {
                if (winVote == 1)
                {
                    if (player == "") ah.SayBattle("vote successful - boss removed");
                    else ah.SayBattle("vote successful - new boss is " + player);
                    ah.BossName = player;
                }
                else ah.SayBattle("not enough votes");
                return true;
            }
            else return false;
        }
    }

    public class VoteSetOptions: AbstractPoll, IVotable
    {
        string scriptTagsFormat;
        string wordFormat;

        public VoteSetOptions(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        public bool Init(TasSayEventArgs e, string[] words)
        {
            wordFormat = Utils.Glue(words);
            scriptTagsFormat = ah.GetOptionsString(e, words);
            if (scriptTagsFormat == "") return false;
            else
            {
                ah.SayBattle("Do you want to apply options " + wordFormat + "? !vote 1 = yes, !vote 2 = no");
                return true;
            }
        }


        public bool Vote(TasSayEventArgs e, string[] words)
        {
            int vote;
            if (!RegisterVote(e, words, out vote))
            {
                AutoHost.Respond(tas, spring, e, "You must vote valid option/not be a spectator");
                return false;
            }

            int winVote;
            if (CheckEnd(out winVote))
            {
                if (winVote == 1)
                {
                    ah.SayBattle("vote successful - appling options " + wordFormat);
                    tas.SetScriptTag(scriptTagsFormat);
                }
                else ah.SayBattle("not enough votes for setoptions");
                return true;
            }
            else return false;
        }
    }
}