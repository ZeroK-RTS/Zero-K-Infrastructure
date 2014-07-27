using System.Collections.Generic;
using System.Linq;
using LobbyClient;

namespace Springie.autohost.Polls
{
    public abstract class AbstractPoll: IVotable
    {
        protected int WinCount { get; private set; }
        protected AutoHost ah;
        protected Spring spring;
        protected TasClient tas;
        bool ended = false;
        protected bool CountNoIntoWinCount = false;

        protected Dictionary<string, bool> userVotes = new Dictionary<string, bool>();

        public string Question { get; private set; }

        public AbstractPoll(TasClient tas, Spring spring, AutoHost ah) {
            this.tas = tas;
            this.spring = spring;
            this.ah = ah;
        }

        protected virtual bool AllowVote(TasSayEventArgs e) {
            return true;
        }


        protected abstract bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount);
        protected abstract void SuccessAction();


        public bool Setup(TasSayEventArgs e, string[] words) {
            string question;
            int winCount;
            if (PerformInit(e, words, out question, out winCount)) {
                WinCount = winCount;
                Question = question;
                if (WinCount <= 0) WinCount = tas.MyBattle != null ? (tas.MyBattle.NonSpectatorCount/2 + 1) : 1;
                if (WinCount <= 0) WinCount = 1;
                // If vote is started by a spec while there are players present don't let the number go below 2.
                if (WinCount <= 1 && tas.MyBattle.NonSpectatorCount != 0 &&
                        tas.MyBattle.Users.Any(u => u.Name == e.UserName && u.IsSpectator))
                    WinCount = 2;
                ah.SayBattle(string.Format("Poll: {0} [!y=0/{1}, !n=0/{1}]", Question, WinCount));
                return true;
            }
            else return false;
        }

        public virtual void End() {
            if (!ended) ah.SayBattle(string.Format("Poll: {0} [END:FAILED]", Question));
            ended = true; // silly hack to avoid duplicate messages 
        }


        public virtual bool Vote(TasSayEventArgs e, bool vote) {
            if (AllowVote(e)) {
                userVotes[e.UserName] = vote;
                var yes = userVotes.Count(x => x.Value == true);
                var no = userVotes.Count(x => x.Value == false);
                ah.SayBattle(string.Format("Poll: {0} [!y={1}/{3}, !n={2}/{3}]", Question, yes, no, CountNoIntoWinCount ? WinCount + no : WinCount));
                if ((!CountNoIntoWinCount && yes >= WinCount) || (CountNoIntoWinCount && yes >= WinCount + no)) {
                    ah.SayBattle(string.Format("Poll: {0} [END:SUCCESS]", Question));
                    ended = true;
                    SuccessAction();
                    return true;
                }
                else if (no >= WinCount) {
                    End();
                    return true;
                }
            }
            else {
                ah.Respond(e, "You are not allowed to vote");
                return false;
            }
            return false;
        }
    };
}