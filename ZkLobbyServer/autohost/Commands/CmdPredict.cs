using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdPredict : ServerBattleCommand
    {
        public override string Help => "predicts chances of victory";
        public override string Shortcut => "predict";
        public override BattleCommandAccess Access => BattleCommandAccess.NoCheck;

        public override ServerBattleCommand Create() => new CmdPredict();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            return String.Empty;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            var b = battle;
            var grouping = b.Users.Values.Where(u => !u.IsSpectator).GroupBy(u => u.AllyNumber).ToList();
            bool is1v1 = grouping.Count == 2 && grouping[0].Count() == 1 && grouping[1].Count() == 1;
            IGrouping<int, UserBattleStatus> oldg = null;
            foreach (var g in grouping)
            {
                if (oldg != null)
                {
                    var t1elo = oldg.Average(x => (is1v1 ? x.LobbyUser.Effective1v1Elo : x.LobbyUser.EffectiveElo));
                    var t2elo = g.Average(x => (is1v1 ? x.LobbyUser.Effective1v1Elo : x.LobbyUser.EffectiveElo));
                    await battle.Respond(e,
                        $"team {oldg.Key + 1} has {ZkData.Utils.GetWinChancePercent(t2elo - t1elo)}% chance to win over team {g.Key + 1}");
                }
                oldg = g;
            }
        }
    }
}