using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;
using Ratings;

namespace ZkLobbyServer
{
    public class CmdPredict: BattleCommand
    {
        public override AccessType Access => AccessType.NoCheck;
        public override string Help => "predicts chances of victory";
        public override string Shortcut => "predict";

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            /*if (!battle.IsMatchMakerBattle)
            {
                battle.Respond(e, "Not a matchmaker battle, cannot predict");
                return null;
            }*/
            return string.Empty;
        }

        public override BattleCommand Create() => new CmdPredict();


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            var b = battle;
            List<IEnumerable<Account>> teams;

            RatingCategory cat = RatingCategory.Casual;
            if (b.IsMatchMakerBattle) cat = RatingCategory.MatchMaking;
            if (b.Mode == PlasmaShared.AutohostMode.Planetwars) cat = RatingCategory.Planetwars;

            using (var db = new ZkDataContext())
            {
                if (battle.IsInGame)
                    teams = b.spring.LobbyStartContext?.Players.Where(u => !u.IsSpectator)
                        .GroupBy(u => u.AllyID)
                        .Select(x => x.Select(p => Account.AccountByName(db, p.Name))).ToList();
                else
                    teams = b.Users.Values.Where(u => !u.IsSpectator)
                        .GroupBy(u => u.AllyNumber)
                        .Select(x => x.Select(p => Account.AccountByName(db, p.Name))).ToList();

                if (teams.Count < 2)
                {
                    await battle.Respond(e, $"!predict needs at least two human teams to work");
                    return;
                }

                var chances = RatingSystems.GetRatingSystem(cat).PredictOutcome(teams);
                for (int i = 0; i < teams.Count; i++)
                {
                    await battle.Respond(e, $"Team {teams[i].First().Name} has a {Math.Round(1000 * chances[i]) / 10}% chance to win");
                }
            }
        }
    }
}