#region using

using System.Collections.Generic;
using System.Linq;
using LobbyClient;

#endregion

namespace Springie.autohost
{
    public interface IVotable
    {
        bool Init(TasSayEventArgs e, string[] words);
        void TimeEnd();
        bool Vote(TasSayEventArgs e, string[] words);
    }

    public abstract class AbstractPoll
    {
        public virtual double Ratio { get { return 0.5; } }

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

        public AbstractPoll() {}

        public AbstractPoll(TasClient tas, Spring spring, AutoHost ah)
        {
            this.tas = tas;
            this.spring = spring;
            this.ah = ah;

            initialUserCount = 0;
            Battle b = tas.MyBattle;
            if (b != null)
            {
                foreach (UserBattleStatus us in b.Users)
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
            foreach (int val in votes) if (val > 0 && val <= options) sums[val - 1]++;

            int votesLeft = votes.FindAll(delegate(int t) { return (t == 0); }).Count;
            bool canDecide = false;
            var winLimit = (int)(initialUserCount*Ratio);

            int max = 0;
            int maxCount = 0;
            for (int i = 0; i < sums.Length; ++i) if (sums[i] > max) max = sums[i];
            for (int i = 0; i < sums.Length; ++i) if (sums[i] == max) maxCount++;

            for (int i = 0; i < sums.Length; ++i)
            {
                string text = string.Format("option {0} has {1} of {2} votes", i + 1, sums[i], winLimit + 1);

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

                int ind = users.IndexOf(e.UserName);
                Battle b = tas.MyBattle;
                if (b != null)
                {
                    int bidx = b.GetUserIndex(e.UserName);
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
    } ;

    public class VoteMap: AbstractPoll, IVotable
    {
        string map;

        public VoteMap(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        bool IVotable.Init(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                AutoHost.Respond(tas, spring, e, "You must specify map name");
                return false;
            }

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
                    ah.SayBattle("vote successful - changing map to " + map);
                    var mapi = ah.cache.GetResourceDataByInternalName(map);
                    if (mapi != null) tas.ChangeMap(mapi.InternalName, mapi.SpringHashes.Where(x=>x.SpringVersion == ah.springPaths.SpringVersion).Select(x=>x.SpringHash).FirstOrDefault());
                }
                else ah.SayBattle("not enough votes, map stays");
                return true;
            }
            else return false;
        }
    }

    public class VoteKick: AbstractPoll, IVotable
    {
        public override double Ratio { get { return 0.66; } }

        string player;

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


    public class VoteSpec : AbstractPoll, IVotable
    {
      string player;

      public VoteSpec(TasClient tas, Spring spring, AutoHost ah) : base(tas, spring, ah) { }

      public bool Init(TasSayEventArgs e, string[] words)
      {
        if (words.Length == 0) {
          AutoHost.Respond(tas, spring, e, "You must specify player name");
          return false;
        }

        string[] players;
        int[] indexes;
        if (AutoHost.FilterUsers(words, tas, spring, out players, out indexes) > 0) {
          player = players[0];
          ah.SayBattle("Do you want to spectate " + player + "? !vote 1 = yes, !vote 2 = no");
          return true;
        } else {
          AutoHost.Respond(tas, spring, e, "Cannot find such player");
          return false;
        }
      }

      public bool Vote(TasSayEventArgs e, string[] words)
      {
        int vote;
        if (!RegisterVote(e, words, out vote)) {
          AutoHost.Respond(tas, spring, e, "You must vote valid option/not be a spectator");
          return false;
        }

        int winVote;
        if (CheckEnd(out winVote)) {
          if (winVote == 1) {
            ah.SayBattle("vote successful - speccing " + player);
            ah.ComForceSpectator(TasSayEventArgs.Default, new[] { player });
          } else ah.SayBattle("not enough votes, player stays");
          return true;
        } else return false;
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
        int allowedAlly = -1;

        public VoteForceStart(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        public bool Init(TasSayEventArgs e, string[] words)
        {
            if (!spring.IsRunning)
            {
                ah.SayBattle("Do you want to force start game? !vote 1 = yes, !vote 2 = no");

                // only smallest ally team can vote
                Battle b = tas.MyBattle;
                List<IGrouping<int, UserBattleStatus>> grouping =
                    b.Users.Where(u => !u.IsSpectator).GroupBy(u => u.AllyNumber).OrderBy(g => g.Count()).ToList();
                if (grouping.Count() == 2)
                {
                    allowedAlly = grouping[0].Key;
                    initialUserCount = grouping[0].Count();
                }
                else allowedAlly = -1;

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
            if (allowedAlly != -1)
            {
                int ally = tas.MyBattle.Users.Where(u => u.Name == e.UserName && !u.IsSpectator).Select(u => u.AllyNumber).SingleOrDefault();
                if (ally != allowedAlly)
                {
                    AutoHost.Respond(tas, spring, e, "Only smaller team can vote");
                    return false;
                }
            }

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
                if (AutoHost.FilterMods(words, ah,out mods, out indexes) == 0)
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