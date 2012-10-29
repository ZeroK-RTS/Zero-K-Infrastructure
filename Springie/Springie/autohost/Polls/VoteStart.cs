using System.Threading;
using LobbyClient;
using System.Linq;

namespace Springie.autohost.Polls
{
    public class VoteStart: AbstractPoll
    {
        
        public VoteStart(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = null;
            if (!spring.IsRunning) {
                question = "Start game? ";

                var invalid = tas.MyBattle.Users.Where(x => !x.IsSpectator && (x.SyncStatus != SyncStatuses.Synced || x.LobbyUser.IsAway)).ToList();
                if (invalid.Count > 0) {
                    foreach (var inv in invalid) {
                        ah.ComRing(e, new []{inv.Name});
                    }
                    ah.SayBattle(string.Join(",", invalid) + " will be forced spectators if they don't download their maps and stop being afk when vote ends");
                }

                winCount = tas.MyBattle.Users.Count(x => !x.IsSpectator) / 2 + 1;
                return true;
            }
            else
            {
                AutoHost.Respond(tas, spring, e, "battle already started");
                return false;
            }
        }

        protected override bool AllowVote(TasSayEventArgs e)
        {
            if (tas.MyBattle == null) return false;
            var entry = tas.MyBattle.Users.FirstOrDefault(x => x.Name == e.UserName);
            if (entry == null || entry.IsSpectator)
            {
                ah.Respond(e, string.Format("Only players can vote"));
                return false;
            }
            else return true;
        }

        protected override void SuccessAction()
        {
            foreach (var user in tas.MyBattle.Users.Where(x => !x.IsSpectator && (x.SyncStatus != SyncStatuses.Synced || x.LobbyUser.IsAway))) {
                ah.ComForceSpectator(TasSayEventArgs.Default, new string[]{user.Name});
            }
            Thread.Sleep(400); // sleep to register spectating

            ah.ComStart(TasSayEventArgs.Default, new string[] { });
        }
    }
}