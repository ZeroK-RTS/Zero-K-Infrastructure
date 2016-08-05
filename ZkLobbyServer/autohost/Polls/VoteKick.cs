using LobbyClient;
using ZkLobbyServer;

namespace Springie.autohost.Polls
{
    public class VoteKick : AbstractPoll, IVotable
    {
        string player;

        public VoteKick(Spring spring, ServerBattle ah) : base(spring, ah) { }

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
            if (ServerBattle.FilterUsers(new[] { words[0] }, ah, spring, out players, out indexes) > 0)
            {
                string reason = (words.Length > 1 && words[1] != "for") ? " for" : "";
                for (var i = 1; i < words.Length; i++) reason += " " + words[i];
                question = "Kick " + player + reason + "?";
                return true;
            }
            else
            {
                ah.Respond(e, "Cannot find such player");
                return false;
            }
        }

        protected override void SuccessAction()
        {
            ah.ComKick(TasSayEventArgs.Default, new[] { player });
        }

    }
}
