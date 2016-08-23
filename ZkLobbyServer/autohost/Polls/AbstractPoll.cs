using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using ZkLobbyServer;

namespace Springie.autohost.Polls
{
    public abstract class AbstractPoll: IVotable
    {
        protected int WinCount { get; private set; }
        protected ServerBattle ah;
        protected Spring spring;
        bool ended = false;
        protected bool CountNoIntoWinCount = false;

        protected Dictionary<string, bool> userVotes = new Dictionary<string, bool>();

        public string Question { get; private set; }
        public string Creator { get; private set; }

        public AbstractPoll(Spring spring, ServerBattle ah) {
            this.spring = spring;
            this.ah = ah;
        }

        protected virtual bool AllowVote(Say e) {
            return true;
        }


        protected abstract bool PerformInit(Say e, string[] words, out string question, out int winCount);
        protected abstract void SuccessAction();


        public bool Setup(Say e, string[] words) {
            string question;
            int winCount;
            if (PerformInit(e, words, out question, out winCount)) {
                WinCount = winCount;
                Question = question;
                Creator = e.User;
                if (WinCount <= 0) WinCount = (ah.NonSpectatorCount/2 + 1);
                if (WinCount <= 0) WinCount = 1;
                // If vote is started by a spec while there are players present don't let the number go below 2.
                if (WinCount <= 1 && ah.NonSpectatorCount != 0 && ah.Users.Values.All(u => u.Name != e.User || u.IsSpectator))
                    WinCount = 2;

                if (WinCount == 1) {
                    SuccessAction();
                    return false;
                } else if (!Vote(e, true)) {
                    ah.SayBattle(string.Format("Poll: {0} [!y=0/{1}, !n=0/{1}]", Question, WinCount));
                }
                return true;
            }
            else return false;
        }

        public virtual void End() {
            if (!ended) ah.SayBattle(string.Format("Poll: {0} [END:FAILED]", Question));
            ended = true; // silly hack to avoid duplicate messages 
        }


        public virtual bool Vote(Say e, bool vote) {
            if (AllowVote(e)) {
                userVotes[e.User] = vote;
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
