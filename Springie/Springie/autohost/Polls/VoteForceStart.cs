using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VoteForceStart: AbstractPoll
    {
        public VoteForceStart(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

      
        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (!spring.IsRunning)
            {
                question = "Force start game?";
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "battle already started");
                return false;
            }
        }

        protected override void SuccessAction() {
            ah.ComForceStart(TasSayEventArgs.Default, new string[]{});
        }
    }
}