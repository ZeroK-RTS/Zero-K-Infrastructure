using System.Linq;
using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VoteExit: AbstractPoll
    {
        public VoteExit(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}


        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (spring.IsRunning)
            {
                question = "Exit this game?";
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "game not running");
                return false;
            }
        }

        protected override bool AllowVote(TasSayEventArgs e)
        {
            if (spring.IsRunning)
            {
                var entry = spring.StartContext.Players.FirstOrDefault(x => x.Name == e.UserName);
                if (entry == null || entry.IsSpectator)
                {
                    ah.Respond(e, string.Format("You must be a player in the game"));
                    return false;
                }
                else return true;
            }
            return false;
        }

        protected override void SuccessAction() {
            ah.ComExit(TasSayEventArgs.Default, new string[]{});
        }
    }
}