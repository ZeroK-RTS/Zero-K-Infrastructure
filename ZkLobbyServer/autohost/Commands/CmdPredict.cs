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
                    var t1elo = oldg.Average(x => (battle.IsMatchMakerBattle ? x.LobbyUser.EffectiveMmElo : x.LobbyUser.EffectiveElo));
                    var t2elo = g.Average(x => (battle.IsMatchMakerBattle ? x.LobbyUser.EffectiveMmElo : x.LobbyUser.EffectiveElo));
                    await battle.Respond(e,
                        $"team {oldg.Key + 1} has {ZkData.Utils.GetWinChancePercent(t2elo - t1elo)}% chance to win over team {g.Key + 1}");
                }
                oldg = g;
            }
        }
    }
}