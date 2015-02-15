using LobbyClient;
using System.Linq;

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

            UserBattleStatus entry;
            tas.MyBattle.Users.TryGetValue(e.UserName, out entry);
            if (entry == null || entry.IsSpectator)
            {
                ah.Respond(e, string.Format("Only players can vote"));
                return false;
            }
            else return true;
        }


        protected override void SuccessAction() {
            ah.ComForceStart(TasSayEventArgs.Default, new string[]{});
        }
    }
}