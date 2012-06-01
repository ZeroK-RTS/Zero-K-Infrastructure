using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VoteSplitPlayers: AbstractPoll
    {
        public VoteSplitPlayers(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = "Split game into two?";
            return true;
        }

        protected override void SuccessAction()
        {
            ah.ComSplitPlayers(TasSayEventArgs.Default, new string[] { });
        }

    }
}