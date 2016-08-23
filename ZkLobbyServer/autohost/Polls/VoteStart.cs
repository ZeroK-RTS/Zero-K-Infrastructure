using System;
using System.Threading;
using LobbyClient;
using System.Linq;
using ZkLobbyServer;

namespace Springie.autohost.Polls
{
    public class VoteStart: AbstractPoll
    {
        
        public VoteStart(Spring spring, ServerBattle ah): base(spring, ah) {}

        protected override bool PerformInit(Say e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = null;
            CountNoIntoWinCount = false;

            if (!spring.IsRunning) {

                var invalid = ah.Users.Values.Where(x => !x.IsSpectator && (x.SyncStatus != SyncStatuses.Synced || x.LobbyUser.IsAway)).ToList();
                if (invalid.Count > 0) foreach (var inv in invalid) ah.ComRing(e, new[] { inv.Name }); // ring invalids ot notify them

                // people wihtout map and spring map changed in last 2 minutes, dont allow start yet
                if (ah.Users.Values.Any(x=>!x.IsSpectator && x.SyncStatus != SyncStatuses.Synced) && DateTime.Now.Subtract(ah.lastMapChange).TotalSeconds < MainConfig.MapChangeDownloadWait) {
                    var waitTime = (int)(MainConfig.MapChangeDownloadWait - DateTime.Now.Subtract(ah.lastMapChange).TotalSeconds);
                    ah.Respond(e, string.Format("Map was changed and some players don't have it yet, please wait {0} more seconds", waitTime));
                    return false;
                }

                question = "Start game? ";

                if (invalid.Count > 0) {
                    var invalids = string.Join(",", invalid);
                    ah.SayBattle(invalids + " will be forced spectators if they don't download their maps and stop being afk when vote ends");
                    question += string.Format("WARNING, SPECTATES: {0}", invalids);
                }
                if (ah.Users.Values.Count(x => !x.IsSpectator) >= 2)
                {
                  winCount = (ah.Users.Values.Count(x => !x.IsSpectator) + 1) / 2 + 1;
                }
                else
                {
                  winCount = 1;
                }
                return true;
            }
            else
            {
                ah.Respond(e, "battle already started");
                return false;
            }
        }

        protected override bool AllowVote(Say e)
        {
            UserBattleStatus entry;
            ah.Users.TryGetValue(e.User, out entry);
            if (entry == null || entry.IsSpectator)
            {
                ah.Respond(e, string.Format("Only players can vote"));
                return false;
            }
            else return true;
        }

        protected override void SuccessAction()
        {
            ah.ComForceSpectatorAfk(ServerBattle.defaultSay, new string[]{});
            foreach (var user in ah.Users.Values.Where(x => !x.IsSpectator && (x.SyncStatus != SyncStatuses.Synced || x.LobbyUser.IsAway))) {
                ah.ComForceSpectator(ServerBattle.defaultSay, new string[]{user.Name});
            }
            new Thread(()=>
                {
                    Thread.Sleep(500); // sleep to register spectating        
                    ah.ComStart(ServerBattle.defaultSay, new string[] { });
                }).Start();
        }
    }
}