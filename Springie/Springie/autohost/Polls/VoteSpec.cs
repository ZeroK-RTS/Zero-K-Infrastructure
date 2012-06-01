using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VoteSpec: AbstractPoll
    {
        string player;

        public VoteSpec(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (words.Length == 0)
            {
                AutoHost.Respond(tas, spring, e, "You must specify player name");
                return false;
            }

            string[] players;
            int[] indexes;
            if (AutoHost.FilterUsers(words, tas, spring, out players, out indexes) > 0)
            {
                player = players[0];
                question = "Spectate " + player + "?";
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "Cannot find such player");
                return false;
            }
        }

        protected override void SuccessAction() {
            ah.ComForceSpectator(TasSayEventArgs.Default, new[] { player });
        }
    }
}