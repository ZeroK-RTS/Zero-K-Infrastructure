#region using

using System.Collections.Generic;
using System.Linq;
using LobbyClient;

#endregion

namespace Springie.autohost.Polls
{
    public class VoteSetOptions: AbstractPoll
    {
        Dictionary<string,string> scriptTagsFormat;

        public VoteSetOptions(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (ah.config.Mode == PlasmaShared.AutohostMode.Serious)
            {
                AutoHost.Respond(tas, spring, e, "Serious host, no custom options");
                return false;
            }
            if (spring.IsRunning)
            {
                AutoHost.Respond(tas, spring, e, "Cannot set options while the game is running");
                return false;
            }
            else
            {
                var wordFormat = Utils.Glue(words);
                scriptTagsFormat = ah.GetOptionsDictionary(e, words);
                if (scriptTagsFormat.Count==0) return false;
                else
                {
                    question  = "Set option " + wordFormat + "?";
                    return true;
                }
            }
        }

        protected override void SuccessAction() {
            tas.UpdateModOptions(scriptTagsFormat);
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
