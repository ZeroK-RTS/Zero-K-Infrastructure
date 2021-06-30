using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;
using Ratings;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;

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
                {
                    teams = b.spring.LobbyStartContext?.Players.Where(u => !u.IsSpectator)
                        .GroupBy(u => u.AllyID)
                        .Select(x => x.Select(p => Account.AccountByName(db, p.Name))).ToList();
                }
                else
                {
                    switch (battle.Mode)
                    {
                        case PlasmaShared.AutohostMode.Game1v1:
                            teams = b.Users.Values.Where(u => !u.IsSpectator)
                                .GroupBy(u => u.Name)
                                .Select(x => x.Select(p => Account.AccountByName(db, p.Name))).ToList();
                            break;

                        case PlasmaShared.AutohostMode.Teams:
                            teams = PartitionBalance.Balance(battle.IsCbalEnabled ? Balancer.BalanceMode.ClanWise : Balancer.BalanceMode.Normal, b.Users.Values.Where(u => !u.IsSpectator).Select(x => x.LobbyUser).Select(x => new PartitionBalance.PlayerItem(x.AccountID, db.Accounts.First(a => a.AccountID == x.AccountID).GetBalancerRating(b.ApplicableRating), x.Clan, x.PartyID)).ToList())
                                .Players
                                .GroupBy(u => u.AllyID)
                                .Select(x => x.Select(p => db.Accounts.Where(a => a.AccountID == p.LobbyID).FirstOrDefault())).ToList();
                            break;

                        default:
                            teams = b.Users.Values.Where(u => !u.IsSpectator)
                                .GroupBy(u => u.AllyNumber)
                                .Select(x => x.Select(p => Account.AccountByName(db, p.Name))).ToList();
                            break;
                    }
                }

                if (teams.Count < 2)
                {
                    await battle.SayBattle($"!predict needs at least two human teams to work");
                    return;
                }

                var chances = RatingSystems.GetRatingSystem(cat).PredictOutcome(teams, DateTime.UtcNow);
                for (int i = 0; i < teams.Count; i++)
                {
                    await battle.SayBattle( $"Team {teams[i].OrderByDescending(x => x.GetRating(cat).RealElo).Select(x => x.Name).Aggregate((a, y) => a + ", " + y)} has a {Math.Round(1000 * chances[i]) / 10}% chance to win");
                }
            }
        }
    }
}