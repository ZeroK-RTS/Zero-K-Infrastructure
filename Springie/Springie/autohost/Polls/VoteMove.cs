using System.Linq;
using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VoteMove: AbstractPoll
    {
        public VoteMove(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

      
        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (!spring.IsRunning)
            {
                question = "Start game?";
                winCount = tas.MyBattle.Users.Count(x => !x.IsSpectator) / 2 + 1;
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "battle already started");
                return false;
            }
        }

        protected override bool AllowVote(TasSayEventArgs e) {
            var entry = spring.StartContext.Players.FirstOrDefault(x => x.Name == e.UserName);
            if (entry == null || entry.IsSpectator)
            {
                ah.Respond(e, string.Format("Only players can vote"));
                return false;
            }
            else return true;            
        }

        protected override void SuccessAction() {
            ah.ComStart(TasSayEventArgs.Default, new string[]{});
        }
    }
}