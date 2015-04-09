using System;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace Springie.autohost.Polls
{
    public class VoteMove: AbstractPoll
    {
        public VoteMove(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}


        string host;


        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = (tas.MyBattle != null) ? tas.MyBattle.Users.Count : 1;
            question = null;
            if (words.Length < 1)
            {
                ah.Respond(e, "<target hostname>");
                return false;
            }
            host = words[0];

            var target_battle = tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == host);
            if (target_battle == null)
            {
                ah.Respond(e, string.Format("Host {0} not found", words[0]));
                return false;
            }

            if (target_battle.IsPassworded)
            {
                ah.Respond(e, string.Format("Move forbidden: host {0} is passworded.", words[0]));
                return false;
            }

            question = string.Format("Move all to {0}?", host);
            return true;

        }


        protected override void SuccessAction() {
            try {
                bool val;
                var moves =
                    tas.MyBattle.Users.Values.Where(x => x.Name != tas.MyBattle.Founder.Name)
                       .Where(x => userVotes.TryGetValue(x.Name, out val) && val)
                       .Select(x => new MovePlayerEntry() { BattleHost = host, PlayerName = x.Name })
                       .ToList(); // move those that voted yes
                var serv = GlobalConst.GetSpringieService();
                serv.MovePlayers(tas.UserName, tas.UserPassword, moves);
            } catch (Exception ex) {
                ah.SayBattle(ex.ToString());
            }
        }

        public override void End()
        {
            bool val;
            try {
                var moves =
                    tas.MyBattle.Users.Values.Where(x => x.Name != tas.MyBattle.Founder.Name)
                       .Where(x => userVotes.TryGetValue(x.Name, out val) && val)
                       .Select(x => new MovePlayerEntry() { BattleHost = host, PlayerName = x.Name})
                       .ToList(); // move those that voted yes if there are at least 2
                if (moves.Count > 1) {
                    var serv = GlobalConst.GetSpringieService();
                    serv.MovePlayers(tas.UserName, tas.UserPassword, moves);
                }
            } catch (Exception ex) {
                ah.SayBattle(ex.ToString());
            }

            base.End();
        }
    }
}
