using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Web.Services;
using LobbyClient;
using PlasmaShared;
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
        public BalanceTeamsResult BalanceTeams(BattleContext context, bool isGameStart, int? allyCount, bool? clanWise)
        {
            return Balancer.BalanceTeams(context,isGameStart, allyCount,clanWise);
        }

        [WebMethod]
        public RecommendedMapResult GetRecommendedMap(BattleContext context, bool pickNew)
        {
            return MapPicker.GetRecommendedMap(context,pickNew);
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

        [WebMethod]
        public string GetMapCommands(string mapName) {
            var db = new ZkDataContext();
            return db.Resources.Single(x => x.InternalName == mapName).MapSpringieCommands;
        }

        public class MovePlayerEntry
        {
            public string PlayerName;
            public string BattleHost;
        }

        [WebMethod]
        public void MovePlayers(string autohostName, string autohostPassword, List<MovePlayerEntry> moves) {

            var db = new ZkDataContext();
            var acc = AuthServiceClient.VerifyAccountPlain(autohostName, autohostPassword);
            if (acc == null) throw new Exception("Invalid password");
            var name = autohostName.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            var entry = db.AutohostConfigs.SingleOrDefault(x => x.Login == name);
            if (entry == null) throw new Exception("Not an autohost");

            try {
                PlayerJuggler.SuppressJuggler = true;
                foreach (var m in moves) {
                    var battle = Global.Nightwatch.Tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == m.BattleHost);
                    if (battle == null || (battle.IsLocked || battle.IsPassworded)) continue;
                    Global.Nightwatch.Tas.ForceJoinBattle(m.PlayerName, m.BattleHost);
                }
            } catch (Exception ex) {
                Trace.TraceError("Error while moving players: {0}", ex);
            } finally {
                PlayerJuggler.SuppressJuggler = false;
            }

        }

        [WebMethod]
        public void SplitAutohost(BattleContext context, string password) {
            if (AuthServiceClient.VerifyAccountPlain(context.AutohostName, password) == null) throw new Exception("Invalid password");
            if (context.GetConfig() == null) throw new Exception("Not an autohost");
            Balancer.SplitAutohost(context);
        }


        [WebMethod]
        public JugglerResult JugglePlayers(List<JugglerAutohost> autohosts)
        {
            return PlayerJuggler.JugglePlayers(autohosts);
        }


        public class RectInfo {
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public int Number;
        }

        [WebMethod]
        public void StoreBoxes(BattleContext context, List<RectInfo> rects) {
            var db = new ZkDataContext();
            var map = db.Resources.Single(x => x.InternalName == context.Map && x.TypeID == ResourceType.Map);
            var orgCommands = map.MapSpringieCommands;
            var newCommands = "!clearbox\n";
            foreach (var r in rects.OrderBy(x => x.Number)) {
                if (r.X != 0 || r.Y != 0 || r.Width != 0 || r.Height != 0) {
                    newCommands += string.Format("!addbox {0} {1} {2} {3} {4}\n",r.X,r.Y,r.Width,r.Height, r.Number + 1);
               }
            }

            if (!string.IsNullOrEmpty(orgCommands)) {
                foreach (var line in orgCommands.Lines().Where(x => !string.IsNullOrEmpty(x))) {
                    if (!line.StartsWith("!addbox") && !line.StartsWith("!clearbox") && !line.StartsWith("!corners") && !line.StartsWith("!split "))
                        newCommands += line + "\n";
                }
            }
            map.MapSpringieCommands = newCommands;
            db.SubmitChanges();
        }


    }
}