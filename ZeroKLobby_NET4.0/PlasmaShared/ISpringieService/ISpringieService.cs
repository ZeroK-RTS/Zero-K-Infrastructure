using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace PlasmaShared
{
     [ServiceContract]
    public interface ISpringieService
    {
        [OperationContract]
        PlayerJoinResult AutohostPlayerJoined(BattleContext context, int accountID);

        [OperationContract]
        BalanceTeamsResult BalanceTeams(BattleContext context, bool isGameStart, int? allyCount, bool? clanWise);

        [OperationContract]
        RecommendedMapResult GetRecommendedMap(BattleContext context, bool pickNew);

        [OperationContract]
        SpringBattleStartSetup GetSpringBattleStartSetup(BattleContext context);

        [OperationContract]
        string SubmitSpringBattleResult(BattleContext context,
                                                        string password,
                                                        BattleResult result,
                                                        List<BattlePlayerResult> players,
                                                        List<string> extraData);

        [OperationContract]
        List<AhConfig> GetClusterConfigs(string clusterNode);

        [OperationContract]
        string GetMapCommands(string mapName);

        [OperationContract]
        void MovePlayers(string autohostName, string autohostPassword, List<MovePlayerEntry> moves);

        [OperationContract]
        void SplitAutohost(BattleContext context, string password);

        [OperationContract]
        void StoreBoxes(BattleContext context, List<RectInfo> rects);
    }
}
