using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdPredict: BattleCommand
    {
        public override AccessType Access => AccessType.NoCheck;
        public override string Help => "predicts chances of victory";
        public override string Shortcut => "predict";

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (!battle.IsMatchMakerBattle)
            {
                battle.Respond(e, "Not a matchmaker battle, cannot predict");
                return null;
            }
            return string.Empty;
        }

        public override BattleCommand Create() => new CmdPredict();


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            var b = battle;
            Dictionary<int, double> grouping;

            using (var db = new ZkDataContext())
            {
                if (battle.IsInGame)
                    grouping = b.spring.LobbyStartContext?.Players.Where(u => !u.IsSpectator)
                        .GroupBy(u => u.AllyID)
                        .ToDictionary(x => x.Key, x => x.Average(y => Account.AccountByName(db, y.Name).EffectiveMmElo));
                else
                    grouping = b.Users.Values.Where(u => !u.IsSpectator)
                        .GroupBy(u => u.AllyNumber)
                        .ToDictionary(x => x.Key, x => x.Average(y => y.LobbyUser.EffectiveMmElo));
            }

            KeyValuePair<int, double>? oldg = null;
            foreach (var g in grouping)
            {
                if (oldg != null)
                {
                    var t1elo = oldg.Value.Value;
                    var t2elo = g.Value;
                    await
                        battle.Respond(e,
                            $"team {oldg.Value.Key + 1} has {Utils.GetWinChancePercent(t2elo - t1elo)}% chance to win over team {g.Key + 1}");
                }
                oldg = g;
            }
        }
    }
}