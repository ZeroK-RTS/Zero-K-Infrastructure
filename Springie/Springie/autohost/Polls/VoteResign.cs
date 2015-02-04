using System.Linq;
using LobbyClient;
using PlasmaShared;

namespace Springie.autohost.Polls
{
    public class VoteResign: AbstractPoll
    {
        BattleContext context;
        PlayerTeam voteStarter;
        public VoteResign(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount) {
            if (spring.IsRunning)
            {
                context = spring.StartContext;
                voteStarter = context.Players.FirstOrDefault(x => x.Name == e.UserName && !x.IsSpectator);
                if (voteStarter != null)
                {
                    question = string.Format("Resign team {0}?", voteStarter.AllyID + 1);
                    int cnt = 0;
                    foreach (var p in context.Players.Where(x => x.AllyID == voteStarter.AllyID && !x.IsSpectator))
                    {
                        if (p.IsIngame || tas.MyBattle.Users.ContainsKey(p.Name))
                        {
                            if (!tas.ExistingUsers[p.Name].IsAway) cnt++;
                        }
                    }
                    winCount = cnt / 2 + 1;
                    return true;
                }
            }
            AutoHost.Respond(tas, spring, e, "You cannot resign now");
            question = null;
            winCount = 0;
            return false;
        }

        protected override bool AllowVote(TasSayEventArgs e)
        {
            if (spring.IsRunning)
            {
                var entry = spring.StartContext.Players.FirstOrDefault(x => x.Name == e.UserName);
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