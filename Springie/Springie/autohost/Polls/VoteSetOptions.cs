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
            var wordFormat = Utils.Glue(words);
            scriptTagsFormat = ah.GetOptionsString(e, words);
            if (scriptTagsFormat == "") return false;
            else
            {
                question  = "Set option " + wordFormat + "?";
                return true;
            }
        }

        protected override void SuccessAction() {
            tas.SetScriptTag(scriptTagsFormat);
        }
    }
}