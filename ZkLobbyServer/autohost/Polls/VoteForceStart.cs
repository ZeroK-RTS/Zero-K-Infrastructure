using LobbyClient;
using System.Linq;
using ZkLobbyServer;

namespace Springie.autohost.Polls
{
    public class VoteForceStart: AbstractPoll
    {
        public VoteForceStart(Spring spring, ServerBattle ah): base(spring, ah) {}

      
        protected override bool PerformInit(Say e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (!spring.IsRunning)
            {
                question = "Force start game?";
                winCount = ah.Users.Values.Count(x => !x.IsSpectator) / 2 + 1;
                return true;
            }
            else
            {
                ah.Respond(e, "battle already started");
                return false;
            }
        }

        protected override bool AllowVote(Say e)
        {
            UserBattleStatus entry;
            ah.Users.TryGetValue(e.User, out entry);
            if (entry == null || entry.IsSpectator)
            {
                ah.Respond(e, "Only players can vote");
                return false;
            }
            else return true;
        }


        protected override void SuccessAction() {
            ah.ComForceStart(ServerBattle.defaultSay, new string[]{});
        }
    }
}