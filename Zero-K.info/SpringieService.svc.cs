using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZeroKWeb
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "SpringieService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select SpringieService.svc or SpringieService.svc.cs at the Solution Explorer and start debugging.
    public class SpringieService : ISpringieService
    {
        
        public PlayerJoinResult AutohostPlayerJoined(BattleContext context, int accountID)
        {
            return PlayerJoinHandler.AutohostPlayerJoined(context, accountID);
        }

        
        public BalanceTeamsResult BalanceTeams(BattleContext context, bool isGameStart, int? allyCount, bool? clanWise)
        {
            return Balancer.BalanceTeams(context, isGameStart, allyCount, clanWise);
        }

        
        public RecommendedMapResult GetRecommendedMap(BattleContext context, bool pickNew)
        {
            return MapPicker.GetRecommendedMap(context, pickNew);
        }


        
        public SpringBattleStartSetup GetSpringBattleStartSetup(BattleContext context)
        {
            return StartSetup.GetSpringBattleStartSetup(context);
        }

        
        public string SubmitSpringBattleResult(BattleContext context,
                                               string password,
                                               BattleResult result,
                                               List<BattlePlayerResult> players,
                                               List<string> extraData)
        {
            return BattleResultHandler.SubmitSpringBattleResult(context, password, result, players, extraData);
        }

        
        public List<AhConfig> GetClusterConfigs(string clusterNode)
        {
            var db = new ZkDataContext();
            return db.AutohostConfigs.Where(x => x.ClusterNode == clusterNode).ToList().Select(x => new AhConfig(x)).ToList();
        }

        
        public string GetMapCommands(string mapName)
        {
            var db = new ZkDataContext();
            return db.Resources.Single(x => x.InternalName == mapName).MapSpringieCommands;
        }


        public void MovePlayers(string autohostName, string autohostPassword, List<MovePlayerEntry> moves)
        {

            var db = new ZkDataContext();
            var acc = AuthServiceClient.VerifyAccountPlain(autohostName, autohostPassword);
            if (acc == null) throw new Exception("Invalid password");
            var name = autohostName.TrimNumbers();
            var entry = db.AutohostConfigs.SingleOrDefault(x => x.Login == name);
            if (entry == null) throw new Exception("Not an autohost");

            try
            {
                foreach (var m in moves)
                {
                    Global.Nightwatch.Tas.ForceJoinBattle(m.PlayerName, m.BattleHost);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while moving players: {0}", ex);
            }
        }

        
        public void SplitAutohost(BattleContext context, string password)
        {
            if (AuthServiceClient.VerifyAccountPlain(context.AutohostName, password) == null) throw new Exception("Invalid password");
            if (context.GetConfig() == null) throw new Exception("Not an autohost");
            Balancer.SplitAutohost(context);
        }


        public void StoreBoxes(BattleContext context, List<RectInfo> rects)
        {
            var db = new ZkDataContext();
            var map = db.Resources.Single(x => x.InternalName == context.Map && x.TypeID == ResourceType.Map);
            var orgCommands = map.MapSpringieCommands;
            var newCommands = "!clearbox\n";
            foreach (var r in rects.OrderBy(x => x.Number))
            {
                if (r.X != 0 || r.Y != 0 || r.Width != 0 || r.Height != 0)
                {
                    newCommands += string.Format("!addbox {0} {1} {2} {3} {4}\n", r.X, r.Y, r.Width, r.Height, r.Number + 1);
                }
            }

            if (!string.IsNullOrEmpty(orgCommands))
            {
                foreach (var line in orgCommands.Lines().Where(x => !string.IsNullOrEmpty(x)))
                {
                    if (!line.StartsWith("!addbox") && !line.StartsWith("!clearbox") && !line.StartsWith("!corners") && !line.StartsWith("!split "))
                        newCommands += line + "\n";
                }
            }
            map.MapSpringieCommands = newCommands;
            db.SubmitChanges();
        }
    }
}
