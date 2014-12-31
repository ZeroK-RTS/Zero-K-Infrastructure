using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VoteForce: AbstractPoll
    {
        public VoteForce(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        override protected bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = null;
            if (spring.IsRunning)
            {
                if (spring.IsPreGame)
                {
                    question = "Force game?";
                    return true;
                }
                else
                {
                    AutoHost.Respond(tas, spring, e, "Battle is already ongoing");
                    return false;
                }
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "Battle has not started yet");
                return false;
            }
        }

        protected override void SuccessAction() {
            ah.ComForce(TasSayEventArgs.Default, new string[]{});
        }
    }
}
