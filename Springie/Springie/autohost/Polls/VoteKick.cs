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
            if (AutoHost.FilterUsers(new[] {words[0]}, tas, spring, out players, out indexes) > 0)
            {
                player = players[0];
                if (player == tas.UserName)
                {
                    ah.Respond(e, "won't kick myself, not in suicidal mood today");
                    return false;
                }
                else
                {
                    string reason = (words.Length > 1 && words[1] != "for") ? " for" : "";
                    for (var i = 1; i < words.Length; i++) reason += " " + words[i];
                    question = "Kick " + player + reason + "?";
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

        protected override bool AllowVote(TasSayEventArgs e)
        {
            if (tas.MyBattle == null) return false;
            var entry = tas.MyBattle.Users.Values.FirstOrDefault(x => x.Name == e.UserName);
            if (entry == null || entry.IsSpectator)
            {
                ah.Respond(e, string.Format("Only players can vote"));
                return false;
            }
            else return true;
        }
    }
}
