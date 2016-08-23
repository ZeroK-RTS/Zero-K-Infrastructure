using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkLobbyServer;

namespace Springie.autohost.Polls
{
    public class VoteExit: AbstractPoll
    {
        public VoteExit(Spring spring, ServerBattle ah): base(spring, ah) {}
        BattleContext context;

        protected override bool PerformInit(Say e, string[] words, out string question, out int winCount) {
            winCount = 0;
            question = null;
            if (spring.IsRunning)
            {
                question = "Exit this game?";
                int cnt = 0;
                context = spring.StartContext;
                foreach (var p in context.Players.Where(x => !x.IsSpectator))
                {
                    if (p.IsIngame || ah.Users.ContainsKey(p.Name))
                    {
                        //Note: "ExistingUsers" is empty if users disconnected from lobby but still ingame.

                        bool afk = ah.server.ConnectedUsers.ContainsKey(p.Name) && ah.server.ConnectedUsers[p.Name].User.IsAway;
                        if (!afk) cnt++;
                    }
                }
                winCount = cnt / 2 + 1;
                return true;
            }
            else
            {
                ah.Respond(e, "game not running");
                return false;
            }
        }

        protected override bool AllowVote(Say e)
        {
            if (spring.IsRunning)
            {
                var entry = context.Players.FirstOrDefault(x => x.Name == e.User);
                if (entry == null || entry.IsSpectator)
                {
                    ah.Respond(e, string.Format("You must be a player in the game"));
                    return false;
                }
                else return true;
            }
            return false;
        }

        protected override void SuccessAction() {
            ah.ComExit(ServerBattle.defaultSay, new string[]{});
        }
    }
}