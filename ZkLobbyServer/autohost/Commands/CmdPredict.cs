using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdPredict : BattleCommand
    {
        public override string Help => "predicts chances of victory";
        public override string Shortcut => "predict";
        public override AccessType Access => AccessType.NoCheck;

        public override BattleCommand Create() => new CmdPredict();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (!battle.IsMatchMakerBattle)
            {
                battle.Respond(e, "Not a matchmaker battle, cannot predict");
                return null;
            }
            return String.Empty;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            var b = battle;
            var grouping = b.Users.Values.Where(u => !u.IsSpectator).GroupBy(u => u.AllyNumber).ToList();
            IGrouping<int, UserBattleStatus> oldg = null;
            foreach (var g in grouping)
            {
                if (oldg != null)
                {
                    var t1elo = oldg.Average(x => x.LobbyUser.EffectiveMmElo);
                    var t2elo = g.Average(x => x.LobbyUser.EffectiveMmElo);
                    await battle.Respond(e,
                        $"team {oldg.Key + 1} has {ZkData.Utils.GetWinChancePercent(t2elo - t1elo)}% chance to win over team {g.Key + 1}");
                }
                oldg = g;
            }
        }
    }
}