#region using

using LobbyClient;
using ZkLobbyServer;

#endregion

namespace Springie.autohost.Polls
{
    public class VoteResetOptions: AbstractPoll
    {
        public VoteResetOptions(Spring spring, ServerBattle ah): base(spring, ah) {}

        override protected bool PerformInit(Say e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = "Reset options?";
            return true;
        }

        protected override void SuccessAction()
        {
            ah.ComResetOptions(ServerBattle.defaultSay, new string[0]);
        }
    }
}