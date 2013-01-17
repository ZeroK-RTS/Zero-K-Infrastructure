using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VoteBoss:AbstractPoll
    {
        string player;

        public VoteBoss(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (words.Length == 0)
            {
                if (ah.BossName == "")
                {
                    ah.Respond(e, "there is currently no boss to remove");
                    return false;
                }
                else
                {
                    player = "";
                    question =  "Remove current boss " + ah.BossName + "?";
                    return true;
                }
            }

            string[] players;
            int[] indexes;
            if (AutoHost.FilterUsers(words, tas, spring, out players, out indexes) > 0)
            {
                player = players[0];
                if (player == tas.UserName)
                {
                    ah.Respond(e, "you flatter me, but no");
                    return false;
                }
                else
                {
                    question = "Elect " + player + " for the boss?";
                    return true;
                }
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "Cannot find such player");
                return false;
            }
        }

        protected override void SuccessAction() {
            ah.BossName = player;
        }
    }
}