using System.Linq;
using LobbyClient;

namespace Springie.autohost.Polls
{
    public class VoteMove: AbstractPoll
    {
        public VoteMove(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}


        string host;


        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = null;
            if (words.Length < 1)
            {
                ah.Respond(e, "<target hostname>");
                return false;
            }
            host = words[0];

            if (!tas.ExistingBattles.Values.Any(x => x.Founder.Name == host))
            {
                ah.Respond(e, string.Format("Host {0} not found", words[0]));
                return false;
            }

            question = "Move all to host {0}?";
            return true;

        }


        protected override void SuccessAction()
        {
            ah.ComMove(TasSayEventArgs.Default, new string[] { host });
        }
    }
}