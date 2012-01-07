using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Services;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZeroKWeb
{
    /// <summary>
    /// Summary description for SpringieService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
        // [System.Web.Script.Services.ScriptService]
    public class SpringieService: WebService
    {
        [WebMethod]
        public PlayerJoinResult AutohostPlayerJoined(BattleContext context, int accountID)
        {
            return PlayerJoinHandler.AutohostPlayerJoined(context, accountID);
        }

        [WebMethod]
        public BalanceTeamsResult BalanceTeams(BattleContext context)
        {
            return Balancer.BalanceTeams(context);
        }

        [WebMethod]
        public RecommendedMapResult GetRecommendedMap(BattleContext context)
        {
            return MapPicker.GetRecommendedMap(context);
        }


        [WebMethod]
        public SpringBattleStartSetup GetSpringBattleStartSetup(BattleContext context)
        {
            return StartSetup.GetSpringBattleStartSetup(context);
        }

        [WebMethod]
        public string SubmitSpringBattleResult(BattleContext context,
                                               string password,
                                               BattleResult result,
                                               List<BattlePlayerResult> players,
                                               List<string> extraData)
        {
            return BattleResultHandler.SubmitSpringBattleResult(context, password, result, players, extraData);
        }

        [WebMethod]
        public List<AhConfig> GetClusterConfigs(string clusterNode)
        {
            var db = new ZkDataContext();
            return db.AutohostConfigs.Where(x => x.ClusterNode == clusterNode).Select(x => new AhConfig(x)).ToList();
        }

    }
}