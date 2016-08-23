#region using

using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using ZkLobbyServer;

#endregion

namespace Springie.autohost.Polls
{
    public class VoteSetOptions: AbstractPoll
    {
        Dictionary<string,string> scriptTagsFormat;

        public VoteSetOptions(Spring spring, ServerBattle ah): base(spring, ah) {}

        protected override bool PerformInit(Say e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (spring.IsRunning)
            {
                ah.Respond(e, "Cannot set options while the game is running");
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

        protected override void SuccessAction()
        {
            ah.server.ConnectedUsers[ah.FounderName].Process(new SetModOptions() { Options = scriptTagsFormat });
        }
    }
}