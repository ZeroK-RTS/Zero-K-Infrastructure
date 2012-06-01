using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VoteRehost: AbstractPoll
    {
        string modname = "";

        public VoteRehost(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}


        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (words.Length == 0)
            {
                question  = "Rehost this game?";
                return true;
            }
            else
            {
                string[] mods;
                int[] indexes;
                if (AutoHost.FilterMods(words, ah, out mods, out indexes) == 0)
                {
                    AutoHost.Respond(tas, spring, e, "cannot find such mod");
                    return false;
                }
                else
                {
                    modname = mods[0];
                    question = "Rehost this game to " + modname + "?";
                    return true;
                }
            }
        }

        protected override void SuccessAction() {
            ah.ComRehost(TasSayEventArgs.Default, new[] { modname });
        }
    }
}