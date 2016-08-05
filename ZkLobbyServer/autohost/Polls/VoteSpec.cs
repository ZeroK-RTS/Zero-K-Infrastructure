using LobbyClient;
using ZkLobbyServer;

namespace Springie.autohost.Polls
{
    public class VoteSpec: AbstractPoll
    {
        string player;

        public VoteSpec(Spring spring, ServerBattle ah): base(spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (words.Length == 0)
            {
                ah.Respond(e, "You must specify player name");
                return false;
            }

            string[] players;
            int[] indexes;
            if (ServerBattle.FilterUsers(words, ah, spring, out players, out indexes) > 0)
            {
                player = players[0];
                question = "Spectate " + player + "?";
                return true;
            }
            else
            {
                ah.Respond(e, "Cannot find such player");
                return false;
            }
        }

        protected override void SuccessAction() {
            ah.ComForceSpectator(TasSayEventArgs.Default, new[] { player });
        }
    }
}