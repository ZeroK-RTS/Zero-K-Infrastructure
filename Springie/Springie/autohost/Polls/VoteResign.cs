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
                    int cnt = 0, total = 0;
                    foreach (var p in context.Players.Where(x => x.AllyID == voteStarter.AllyID && !x.IsSpectator))
                    {
                        total++;
                        if (p.IsIngame || tas.MyBattle.Users.ContainsKey(p.Name))
                        {
                            //Note: "ExistingUsers" is empty if users disconnected from lobby but still ingame.

                            bool afk = tas.ExistingUsers.ContainsKey(p.Name) && tas.ExistingUsers[p.Name].IsAway;
                            if (!afk) cnt++;
                        }
                    }
                    winCount = (cnt * 3 / 5) + 1;
                    if (total > 1 && winCount == 1) winCount = 2; // prevents most pathological cases (like a falsely AFK partner in 2v2)
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
