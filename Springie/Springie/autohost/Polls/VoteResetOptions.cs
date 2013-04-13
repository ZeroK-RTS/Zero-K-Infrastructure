#region using

using LobbyClient;

#endregion

namespace Springie.autohost.Polls
{
    public class VoteResetOptions: AbstractPoll
    {
        public VoteResetOptions(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        override protected bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = "Reset options?";
            return true;
        }

        protected override void SuccessAction()
        {
            ah.ComResetOptions(TasSayEventArgs.Default, new string[0]);
        }
    }
}