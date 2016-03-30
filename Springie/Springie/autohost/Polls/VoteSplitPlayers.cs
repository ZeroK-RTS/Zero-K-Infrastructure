using LobbyClient;
using System.Linq;

namespace Springie.autohost.Polls
{
    public class VoteSplitPlayers: AbstractPoll
    {
        public VoteSplitPlayers(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = null;
            if (!spring.IsRunning)
            {
                question = "Split the game into two?";
                winCount = tas.MyBattle.Users.Values.Count(x => !x.IsSpectator) / 2 + 1;
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "battle already started");
                return false;
            }
        }

        protected override bool AllowVote(TasSayEventArgs e)
        {
            if (tas.MyBattle == null) return false;
            return true;
        }


        protected override void SuccessAction()
        {
            ah.ComSplitPlayers(TasSayEventArgs.Default, new string[] { });
        }

    }
}
