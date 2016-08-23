using System;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;
using ZkLobbyServer;

namespace Springie.autohost.Polls
{
    public class VoteResign: AbstractPoll
    {
        BattleContext context;
        PlayerTeam voteStarter;
        public VoteResign(Spring spring, ServerBattle ah): base(spring, ah) {}

        protected override bool PerformInit(Say e, string[] words, out string question, out int winCount) {
            if (spring.IsRunning)
            {
                context = spring.StartContext;

                if (DateTime.UtcNow.Subtract(spring.IngameStartTime ?? DateTime.Now).TotalSeconds < GlobalConst.MinDurationForElo)
                {
                    ah.Respond(e,"You cannot resign so early");
                    question = null;
                    winCount = 0;
                    return false;
                }

                voteStarter = context.Players.FirstOrDefault(x => x.Name == e.User && !x.IsSpectator);
                if (voteStarter != null)
                {
                    question = string.Format("Resign team {0}?", voteStarter.AllyID + 1);
                    int cnt = 0, total = 0;
                    foreach (var p in context.Players.Where(x => x.AllyID == voteStarter.AllyID && !x.IsSpectator))
                    {
                        total++;
                        if (p.IsIngame || ah.Users.ContainsKey(p.Name))
                        {
                            //Note: "ExistingUsers" is empty if users disconnected from lobby but still ingame.

                            bool afk = ah.server.ConnectedUsers.ContainsKey(p.Name) && ah.server.ConnectedUsers[p.Name].User.IsAway;
                            if (!afk) cnt++;
                        }
                    }
                    winCount = (cnt * 3 / 5) + 1;
                    if (total > 1 && winCount == 1) winCount = 2; // prevents most pathological cases (like a falsely AFK partner in 2v2)
                    return true;
                }
            }
            ah.Respond(e, "You cannot resign now");
            question = null;
            winCount = 0;
            return false;
        }

        protected override bool AllowVote(Say e)
        {
            if (spring.IsRunning)
            {
                var entry = spring.StartContext.Players.FirstOrDefault(x => x.Name == e.User);
                if (entry == null || entry.IsSpectator || entry.AllyID != voteStarter.AllyID)
                {
                    ah.Respond(e, string.Format("Only team {0} can vote", voteStarter.AllyID + 1));
                    return false;
                }
                else return true;
            }
            return false;
        }


        protected override void SuccessAction() {
            if (spring.IsRunning) foreach (var p in context.Players.Where(x => x.AllyID == voteStarter.AllyID && !x.IsSpectator)) spring.ResignPlayer(p.Name);
        }
    }
}
