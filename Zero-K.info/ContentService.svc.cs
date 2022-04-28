using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LobbyClient;
using Microsoft.Linq.Translations;
using PlasmaDownloader;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "ContentService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select ContentService.svc or ContentService.svc.cs at the Solution Explorer and start debugging.
    public class ContentService : IContentService
    {
        
        public DownloadFileResponse DownloadFile(string internalName)
        {
            return PlasmaServer.DownloadFile(internalName);
        }

        public List<string> GetEngineList(string platform)
        {
            var comparer = new EngineDownload.VersionNumberComparer();
            var list = new DirectoryInfo(Path.Combine(Global.MapPath("~"), "engine", platform ?? "win32")).GetFiles().Select(x => x.Name).Select(Path.GetFileNameWithoutExtension).OrderBy(x => x, comparer).ToList();
            return list;
        }

        public string GetDefaultEngine()
        {
            return MiscVar.DefaultEngine ?? GlobalConst.DefaultEngineOverride;
        }



        public List<ResourceData> FindResourceData(string[] words, ResourceType? type = null)
        {
            var db = new ZkDataContext();
            var ret = db.Resources.AsQueryable();
            if (type == ResourceType.Map) ret = ret.Where(x => x.TypeID == ResourceType.Map);
            if (type == ResourceType.Mod) ret = ret.Where(x => x.TypeID == ResourceType.Mod);
            string joinedWords = string.Join(" ", words);
            var test = ret.Where(x => x.InternalName == joinedWords);
            if (test.Any()) return test.OrderByDescending(x => x.MapSupportLevel).AsEnumerable().Select(PlasmaServer.ToResourceData).ToList();
            int i;
            if (words.Length == 1 && int.TryParse(words[0], out i)) ret = ret.Where(x => x.ResourceID == i);
            else
            {
                foreach (var w in words)
                {
                    var w1 = w;
                    ret = ret.Where(x => SqlFunctions.PatIndex("%" + w1 + "%", x.InternalName) > 0);
                }
            }
            ret = ret.Where(x => x.ResourceContentFiles.Any(y => y.LinkCount > 0));
            return ret.OrderByDescending(x => x.MapSupportLevel).Take(400).ToList().Select(PlasmaServer.ToResourceData).ToList();
        }


        




        /// <summary>
        /// Finds resource by either md5 or internal name
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="internalName"></param>
        /// <returns></returns>
        
        public ResourceData GetResourceData(string md5, string internalName)
        {
            return PlasmaServer.GetResourceData(md5, internalName);
        }

        
        public ResourceData GetResourceDataByInternalName(string internalName)
        {
            var db = new ZkDataContext();
            var entry = db.Resources.SingleOrDefault(x => x.InternalName == internalName);
            if (entry != null) return PlasmaServer.ToResourceData(entry);
            else return null;
        }

        
        public ResourceData GetResourceDataByResourceID(int resourceID)
        {
            var db = new ZkDataContext();
            return PlasmaServer.ToResourceData(db.Resources.Single(x => x.ResourceID == resourceID));
        }


        
        public List<ResourceData> GetResourceList(DateTime? lastChange, out DateTime currentTime)
        {
            return PlasmaServer.GetResourceList(lastChange, out currentTime);
        }


        
        public ScriptMissionData GetScriptMissionData(string name)
        {
            using (var db = new ZkDataContext())
            {
                var m = db.Missions.Single(x => x.Name == name && x.IsScriptMission);
                var mod =
                    db.Resources.Where(x => x.TypeID == ResourceType.Mod && x.RapidTag == m.ModRapidTag)
                        .OrderByDescending(x => x.ResourceID)
                        .FirstOrDefault();
                return new ScriptMissionData()
                {
                    MapName = m.Map,
                    ModName = mod?.InternalName ?? m.ModRapidTag,
                    StartScript = m.Script,
                    ManualDependencies = m.ManualDependencies != null ? new List<string>(m.ManualDependencies.Split('\n')) : null,
                    Name = m.Name
                };
            }
        }


        
        public void NotifyMissionRun(string login, string missionName)
        {
            missionName = Mission.GetNameWithoutVersion(missionName);
            using (var db = new ZkDataContext())
            {
                db.Missions.Single(x => x.Name == missionName).MissionRunCount++;
                Account.AccountByName(db, login).MissionRunCount++;
                db.SaveChanges();
            }
        }


        
        public ReturnValue RegisterResource(int apiVersion,
                                                         string springVersion,
                                                         string md5,
                                                         int length,
                                                         ResourceType resourceType,
                                                         string archiveName,
                                                         string internalName,
                                                         byte[] serializedData,
                                                         List<string> dependencies,
                                                         byte[] minimap,
                                                         byte[] metalMap,
                                                         byte[] heightMap,
                                                         byte[] torrentData)
        {
            return PlasmaServer.RegisterResource(new RegisterResourceRequest(apiVersion, springVersion, md5, length, resourceType, archiveName, internalName, serializedData, dependencies, minimap, metalMap, heightMap, torrentData));
        }

        
        public void SubmitMissionScore(string login, string passwordHash, string missionName, int score, int gameSeconds, string missionVars = "")
        {
            missionName = Mission.GetNameWithoutVersion(missionName);

            using (var db = new ZkDataContext())
            {
                var acc = AuthServiceClient.VerifyAccountHashed(login, passwordHash);
                if (acc == null)
                {
                    Trace.TraceWarning("Invalid login attempt for {0}" , login);
                    System.Threading.Thread.Sleep(new Random().Next(2000));
                }

                acc.Xp += GlobalConst.XpForMissionOrBots;

                var mission = db.Missions.Single(x => x.Name == missionName);

                if (score != 0 || mission.RequiredForMultiplayer)
                {
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
                            acc.Xp += 150; // 150 for getting top score
                        }
                        scoreEntry.Score = score;
                        scoreEntry.Time = DateTime.UtcNow;
                        scoreEntry.MissionRevision = mission.Revision;
                        scoreEntry.GameSeconds = gameSeconds;
                    }
                }

                acc.CheckLevelUp();
                db.SaveChanges();

                if (!acc.CanPlayMultiplayer)
                {
                    if (
                        db.Missions.Where(x => x.RequiredForMultiplayer)
                            .All(y => y.MissionScores.Any(z => z.AccountID == acc.AccountID)))
                    {
                        acc.CanPlayMultiplayer = true;
                        db.SaveChanges();
                        Global.Server.PublishAccountUpdate(acc);
                        Global.Server.GhostPm(acc.Name, "Congratulations! You are now authorized to play MultiPlayer games!");
                    }
                }
            }
        }

        public List<ClientMissionInfo> GetDefaultMissions()
        {
            using (var db = new ZkDataContext())
            {
                var ret = db.Missions.Where(x => x.FeaturedOrder != null && !x.IsDeleted).Select(x =>
                
                    new ClientMissionInfo()
                    {
                        DisplayName = x.Name,
                        Author = x.Account.Name,
                        Revision = x.Revision,
                        Description = x.Description,
                        Script = x.Script,
                        FeaturedOrder = x.FeaturedOrder,
                        Mod = x.ModRapidTag ?? x.Mod,
                        Map = x.Map,
                        IsScriptMission = x.IsScriptMission,
                        Rating = x.Rating,
                        Difficulty = x.Difficulty,
                        MissionID = x.MissionID,
                        DownloadHandle = x.Resources.Select(x1=>x1.InternalName).FirstOrDefault(),
                        OtherDependencies = x.ManualDependencies
                    }
                ).ToList(); // this does in single sql query

                // add image url
                foreach (var m in ret)
                {
                    m.ImageUrl = $"{GlobalConst.BaseSiteUrl}/img/missions/{m.MissionID}.png";
                }

                return ret;
            }
        }

        public PublicCommunityInfo GetPublicCommunityInfo()
        {
            var info = new PublicCommunityInfo();
            info.NewsItems = Global.Server.NewsListManager.GetCurrentNewsList().NewsItems;
            info.LadderItems = Global.Server.LadderListManager.GetCurrentLadderList().LadderItems;
            info.ForumItems = Global.Server.ForumListManager.GetCurrentForumList(null).ForumItems;
            info.UserCountLimited = MiscVar.ZklsMaxUsers > 0;
            info.MapItems = MemCache.GetCached<List<MapItem>>("featuredMapItems",
                () =>
                {
                    using (var db = new ZkDataContext())
                    {
                        return db.Resources.Where(x => x.TypeID == ResourceType.Map && x.MapSupportLevel >= MapSupportLevel.Featured).ToList()
                            .Select(x => x.ToMapItem()).ToList();
                    }
                }, 3542);

            return info;
        }

        public List<CustomGameModeInfo> GetFeaturedCustomGameModes()
        {
            using (var db = new ZkDataContext())
            {
                var ret = db.GameModes.Where(x => x.IsFeatured).Select(x => new CustomGameModeInfo()
                {
                    DisplayName = x.DisplayName, FileContent = x.GameModeJson, FileName = x.ShortName
                }).ToList();

                return ret;
            }
        }

        public SpringBattleInfo GetSpringBattleInfo(string gameid)
        {
            using (var db = new ZkDataContext())
            {
                var sb = db.SpringBattles.FirstOrDefault(x => x.EngineGameID == gameid);
                if (sb == null) return null;

                return new SpringBattleInfo()
                {
                    AutohostMode = sb.Mode,
                    SpringBattleID = sb.SpringBattleID,
                    IsMatchMaker = sb.IsMatchMaker,
                    Title = sb.Title
                };
            }
        }
    }


}
