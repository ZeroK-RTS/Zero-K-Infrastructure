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
    public class SpringieService 
    {
        
        public PlayerJoinResult AutohostPlayerJoined(BattleContext context, int accountID)
        {
            return PlayerJoinHandler.AutohostPlayerJoined(context, accountID);
        }

        
        public BalanceTeamsResult BalanceTeams(BattleContext context, bool isGameStart, int? allyCount, bool? clanWise)
        {
            return Balancer.BalanceTeams(context, isGameStart, allyCount, clanWise);
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

        
       
        public string GetMapCommands(string mapName)
        {
            var db = new ZkDataContext();
            var map = db.Resources.FirstOrDefault(x => x.InternalName == mapName);
            if (map  == null) throw new Exception(string.Format("Map {0} not found in database", mapName));
            return map.MapSpringieCommands;

        }


        public void MovePlayers(string autohostName, string autohostPassword, List<MovePlayerEntry> moves)
        {
            var acc = AuthServiceClient.VerifyAccountPlain(autohostName, autohostPassword);
            if (acc == null) throw new Exception("Invalid password");
            try
            {
                foreach (var m in moves)
                {
                    Global.Server.ForceJoinBattle(m.PlayerName, m.BattleHost);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while moving players: {0}", ex);
            }
        }

      

    }
}
