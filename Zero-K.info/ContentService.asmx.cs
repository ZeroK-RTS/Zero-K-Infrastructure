using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Web.Services;
using PlasmaShared;
using PlasmaShared.ContentService;
using ZeroKWeb;
using ZeroKWeb.Controllers;
using ZeroKWeb.SpringieInterface;
using ZkData;
using BalanceTeamsResult = ZeroKWeb.SpringieInterface.BalanceTeamsResult;
using BattlePlayerResult = ZeroKWeb.SpringieInterface.BattlePlayerResult;
using BattleResult = ZeroKWeb.SpringieInterface.BattleResult;
using BotTeam = ZeroKWeb.SpringieInterface.BotTeam;
using ProgramType = ZkData.ProgramType;
using RecommendedMapResult = ZeroKWeb.SpringieInterface.RecommendedMapResult;
using ResourceType = ZkData.ResourceType;
using SpringBattleStartSetup = ZeroKWeb.SpringieInterface.SpringBattleStartSetup;

namespace ZeroKWeb
{
    /// <summary>
    /// Summary description for ContentService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
        // [System.Web.Script.Services.ScriptService]
    public class ContentService: WebService
    {

        [WebMethod]
        public bool DownloadFile(string internalName,
                                 out List<string> links,
                                 out byte[] torrent,
                                 out List<string> dependencies,
                                 out ResourceType resourceType,
                                 out string torrentFileName)
        {
            return PlasmaServer.DownloadFile(internalName, out links, out torrent, out dependencies, out resourceType, out torrentFileName);
        }

        [WebMethod]
        public List<PlasmaServer.ResourceData> FindResourceData(string[] words, ResourceType? type = null)
        {
            var db = new ZkDataContext();
            var ret = db.Resources.AsQueryable();
            if (type == ResourceType.Map) ret = ret.Where(x => x.TypeID == ResourceType.Map);
            if (type == ResourceType.Mod) ret = ret.Where(x => x.TypeID == ResourceType.Mod);
            var test = ret.Where(x => x.InternalName == string.Join(" ", words));
            if (test.Any()) return test.OrderByDescending(x => -x.FeaturedOrder).Select(x => new PlasmaServer.ResourceData(x)).ToList();
            int i;
            if (words.Length == 1 && int.TryParse(words[0], out i)) ret = ret.Where(x => x.ResourceID == i);
            else
            {
                foreach (var w in words)
                {
                    var w1 = w;
                    ret = ret.Where(x => SqlMethods.Like(x.InternalName, "%" + w1 + "%"));
                }
            }
            return ret.OrderByDescending(x => -x.FeaturedOrder).Take(400).Select(x => new PlasmaServer.ResourceData(x)).ToList();
        }

       
        [WebMethod]
        public List<string> GetEloTop10()
        {
            using (var db = new ZkDataContext())
            return
                db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > DateTime.UtcNow.AddMonths(-1))).OrderByDescending(
                    x => x.Elo).Select(x => x.Name).Take(10).ToList();
        }


    

        /// <summary>
        /// Finds resource by either md5 or internal name
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="internalName"></param>
        /// <returns></returns>
        [WebMethod]
        public PlasmaServer.ResourceData GetResourceData(string md5, string internalName)
        {
            return PlasmaServer.GetResourceData(md5, internalName);
        }

        [WebMethod]
        public PlasmaServer.ResourceData GetResourceDataByInternalName(string internalName)
        {
            var db = new ZkDataContext();
            return new PlasmaServer.ResourceData(db.Resources.Single(x => x.InternalName == internalName));
        }

        [WebMethod]
        public PlasmaServer.ResourceData GetResourceDataByResourceID(int resourceID)
        {
            var db = new ZkDataContext();
            return new PlasmaServer.ResourceData(db.Resources.Single(x => x.ResourceID == resourceID));
        }


        [WebMethod]
        public List<PlasmaServer.ResourceData> GetResourceList(DateTime? lastChange, out DateTime currentTime)
        {
            return PlasmaServer.GetResourceList(lastChange, out currentTime);
        }


        [WebMethod]
        public ScriptMissionData GetScriptMissionData(string name)
        {
            using (var db = new ZkDataContext())
            {
                var m = db.Missions.Single(x => x.Name == name && x.IsScriptMission);
                return new ScriptMissionData()
                       {
                           MapName = m.Map,
                           ModTag = m.ModRapidTag,
                           StartScript = m.Script,
                           ManualDependencies = m.ManualDependencies != null ? new List<string>(m.ManualDependencies.Split('\n')) : null,
                           Name = m.Name
                       };
            }
        }


        [WebMethod]
        public void NotifyMissionRun(string login, string missionName)
        {
            missionName = Mission.GetNameWithoutVersion(missionName);
            using (var db = new ZkDataContext())
            using (var scope = new TransactionScope())
            {
                db.Missions.Single(x => x.Name == missionName).MissionRunCount++;
                Account.AccountByName(db,login).MissionRunCount++;
                db.SubmitChanges();
                scope.Complete();
            }
        }


        [WebMethod]
        public PlasmaServer.ReturnValue RegisterResource(int apiVersion,
                                                         string springVersion,
                                                         string md5,
                                                         int length,
                                                         ResourceType resourceType,
                                                         string archiveName,
                                                         string internalName,
                                                         int springHash,
                                                         byte[] serializedData,
                                                         List<string> dependencies,
                                                         byte[] minimap,
                                                         byte[] metalMap,
                                                         byte[] heightMap,
                                                         byte[] torrentData)
        {
            return PlasmaServer.RegisterResource(apiVersion,
                                                 springVersion,
                                                 md5,
                                                 length,
                                                 resourceType,
                                                 archiveName,
                                                 internalName,
                                                 springHash,
                                                 serializedData,
                                                 dependencies,
                                                 minimap,
                                                 metalMap,
                                                 heightMap,
                                                 torrentData);
        }

        [WebMethod]
        public void SubmitMissionScore(string login, string passwordHash, string missionName, int score, int gameSeconds)
        {
            missionName = Mission.GetNameWithoutVersion(missionName);

            using (var db = new ZkDataContext())
            {
                var acc = AuthServiceClient.VerifyAccountHashed(login, passwordHash);
                if (acc == null) throw new ApplicationException("Invalid login or password");

                acc.XP += GlobalConst.XpForMissionOrBots;

                var mission = db.Missions.Single(x => x.Name == missionName);

                var scoreEntry = mission.MissionScores.FirstOrDefault(x => x.AccountID == acc.AccountID);
                if (scoreEntry == null)
                {
                    scoreEntry = new MissionScore() { MissionID = mission.MissionID, AccountID = acc.AccountID, Score = int.MinValue };
                    mission.MissionScores.Add(scoreEntry);
                }

                if (score > scoreEntry.Score)
                {
                    var max = mission.MissionScores.Max(x => (int?)x.Score);
                    if (max == null || max <= score)
                    {
                        mission.TopScoreLine = login;
                        acc.XP += 150; // 150 for getting top score
                    }
                    scoreEntry.Score = score;
                    scoreEntry.Time = DateTime.UtcNow;
                    scoreEntry.MissionRevision = mission.Revision;
                    scoreEntry.GameSeconds = gameSeconds;
                    db.SubmitChanges();
                }
            }
        }


     

        [WebMethod]
        public void SubmitStackTrace(ProgramType programType, string playerName, string exception, string extraData, string programVersion)
        {
            using (var db = new ZkDataContext())
            {
                var exceptionLog = new ExceptionLog
                                   {
                                       ProgramID = programType,
                                       Time = DateTime.UtcNow,
                                       PlayerName = playerName,
                                       ExtraData = extraData,
                                       Exception = exception,
                                       ExceptionHash = new Hash(exception).ToString(),
                                       ProgramVersion = programVersion,
                                       RemoteIP = GetUserIP()
                                   };
                db.ExceptionLogs.InsertOnSubmit(exceptionLog);
                db.SubmitChanges();
            }
        }


        [WebMethod]
        public bool VerifyAccountData(string login, string password)
        {
            var acc = AuthServiceClient.VerifyAccountPlain(login, password);
            if (acc == null) return false;
            return true;
        }


        string GetUserIP()
        {
            var ip = Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip) || ip.Equals("unknown", StringComparison.OrdinalIgnoreCase)) ip = Context.Request.ServerVariables["REMOTE_ADDR"];
            return ip;
        }


     

        public class EloInfo
        {
            public double Elo = 1500;
            public double Weight = 1;
        }


        public class ScriptMissionData
        {
            public List<string> ManualDependencies;
            public string MapName;
            public string ModTag;
            public string Name;
            public string StartScript;
        }

    }



}