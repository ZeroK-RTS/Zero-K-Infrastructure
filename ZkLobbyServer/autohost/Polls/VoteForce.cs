using LobbyClient;
using ZkLobbyServer;

namespace Springie.autohost.Polls
{
    public class VoteForce: AbstractPoll
    {
        public VoteForce(Spring spring, ServerBattle ah): base(spring, ah) {}

        override protected bool PerformInit(Say e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = null;
            if (spring.IsRunning)
            {
                question = "Force game?";
                return true;
            }
            else
            {
                ah.Respond(e, "battle not started yet");
                return false;
            }
        }

        protected override void SuccessAction() {
            ah.ComForce(ServerBattle.defaultSay, new string[]{});
        }
    }
}