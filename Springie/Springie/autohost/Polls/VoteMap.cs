using LobbyClient;
using PlasmaShared.ContentService;

namespace Springie.autohost.Polls
{
    public class VoteMap: AbstractPoll
    {
        string map;

        public VoteMap(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = null;
			if (spring.IsRunning)
            {
                AutoHost.Respond(tas, spring, e, "Cannot change map while the game is running");
                return false;
            }
            else
            {
                if (words.Length > 0)
                {
                    string[] vals;
                    int[] indexes;
                    ah.FilterMaps(words, out vals, out indexes);
                    if (vals.Length > 0)
                    {
                        map = vals[0];
                        var resource = ah.cache.FindResourceData(new string[]{map}, ResourceType.Map);
                        question = string.Format("Change map to {0} http://zero-k.info/Maps/Detail/{1} ?", map, resource[0].ResourceID);
                        return true;
                    }
                    else
                    {
                        AutoHost.Respond(tas, spring, e, "Cannot find such map");
                        return false;
                    }
                }
                else
                {
                    question = "Do you want to change to a suitable random map?";
                    return true;
                }
            }
        }
		
        protected override void SuccessAction()
        {
            if (string.IsNullOrEmpty(map))
            {
                ah.ComMap(TasSayEventArgs.Default, new string[] { });
            }
            else
            {
                ah.ComMap(TasSayEventArgs.Default, new string[] { map });
            }
        }
    }
}