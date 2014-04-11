#region using

using LobbyClient;

#endregion

namespace Springie.autohost.Polls
{
    public class VoteSetOptions: AbstractPoll
    {
        string scriptTagsFormat;

        public VoteSetOptions(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (spring.IsRunning)
            {
                AutoHost.Respond(tas, spring, e, "Cannot set options while the game is running");
                return false;
            }
            else
            {
                var wordFormat = Utils.Glue(words);
                scriptTagsFormat = ah.GetOptionsString(e, words);
                if (scriptTagsFormat == "") return false;
                else
                {
                    question  = "Set option " + wordFormat + "?";
                    return true;
                }
            }
        }

        protected override void SuccessAction() {
            tas.SetScriptTag(scriptTagsFormat);
        }
    }
}