using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VoteKick: AbstractPoll, IVotable
    {
        string player;

        public VoteKick(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            question = null;
            winCount = 0;
            if (words.Length == 0)
            {
                ah.Respond(e, "You must specify player name");
                return false;
            }

            string[] players;
            int[] indexes;
            if (AutoHost.FilterUsers(words, tas, spring, out players, out indexes) > 0)
            {
                player = players[0];
                if (player == tas.UserName)
                {
                    ah.Respond(e, "won't kick myself, not in suicidal mood today");
                    return false;
                }
                else
                {
                    question = "Kick " + player + "?";
                    return true;
                }
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "Cannot find such player");
                return false;
            }
        }

        protected override void SuccessAction()
        {
            ah.ComKick(TasSayEventArgs.Default, new[] { player });
        }

    }
}