using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VoteStart: AbstractPoll
    {
        public VoteStart(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

      
        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (!spring.IsRunning)
            {
                question = "Start game?";
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "battle already started");
                return false;
            }
        }

        protected override void SuccessAction() {
            ah.ComStart(TasSayEventArgs.Default, new string[]{});
        }
    }
}