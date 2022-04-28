using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlasmaDownloader;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb
{
    public class ContentServiceImplementation: IContentServiceClient
    {
        static CommandJsonSerializer serializer;

        static ContentServiceImplementation()
        {
            serializer = new CommandJsonSerializer(Utils.GetAllTypesWithAttribute<ApiMessageAttribute>());
        }


        public async Task<T> QueryAsync<T>(ApiRequest<T> request) where T: ApiResponse, new()
        {
            var ret = await Process(request) as T;
            return ret;
        }
        
        public T Query<T>(ApiRequest<T> request) where T: ApiResponse, new()
        {
            return QueryAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<string> Process(string request)
        {
            var req = serializer.DeserializeLine(request);
            var response = await Process(req);
            return serializer.SerializeContentOnly(response);
        }

        async Task<ApiResponse> Process(object request)
        {
            dynamic req = request;
            var response = await Process(req);
            return response;
        }

        async Task<DownloadFileResponse> Process(DownloadFileRequest request)
        {
            return PlasmaServer.DownloadFile(request.InternalName);
        }

        async Task<GetEngineListResponse> Process(GetEngineListRequest request)
        {
            var comparer = new EngineDownload.VersionNumberComparer();
            var list = new DirectoryInfo(Path.Combine(Global.MapPath("~"), "engine", request.Platform ?? "win32")).GetFiles().Select(x => x.Name)
                .Select(Path.GetFileNameWithoutExtension).OrderBy(x => x, comparer).ToList();
            return new GetEngineListResponse() { Engines = list };
        }

        async Task<GetDefaultEngineResponse> Process(GetDefaultEngineRequest request)
        {
            var ver = MiscVar.DefaultEngine ?? GlobalConst.DefaultEngineOverride;
            return new GetDefaultEngineResponse() { DefaultEngine = ver };
        }

        async Task<FindResourceDataResponse> Process(FindResourceDataRequest request)
        {
            var words = request.Words;
            var type = request.Type;

            var db = new ZkDataContext();
            var ret = db.Resources.AsQueryable();
            if (type == ResourceType.Map) ret = ret.Where(x => x.TypeID == ResourceType.Map);
            if (type == ResourceType.Mod) ret = ret.Where(x => x.TypeID == ResourceType.Mod);
            string joinedWords = string.Join(" ", words);
            var test = ret.Where(x => x.InternalName == joinedWords);
            if (test.Any())
                new FindResourceDataResponse()
                {
                    Resources = test.OrderByDescending(x => x.MapSupportLevel).AsEnumerable().Select(PlasmaServer.ToResourceData).ToList()
                };
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
            return new FindResourceDataResponse()
            {
                Resources = ret.OrderByDescending(x => x.MapSupportLevel).Take(400).ToList().Select(PlasmaServer.ToResourceData).ToList()
            };
        }

        async Task<ResourceData> Process(GetResourceDataRequest request)
        {
            return PlasmaServer.GetResourceData(request.Md5, request.InternalName);
        }

        async Task<ScriptMissionData> Process(GetScriptMissionDataRequest request)
        {
            using (var db = new ZkDataContext())
            {
                var m = db.Missions.Single(x => x.Name == request.MissionName && x.IsScriptMission);
                var mod = db.Resources.Where(x => x.TypeID == ResourceType.Mod && x.RapidTag == m.ModRapidTag).OrderByDescending(x => x.ResourceID)
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

        async Task<NotifyMissionRunResponse> Process(NotifyMissionRun request)
        {
            var missionName = Mission.GetNameWithoutVersion(request.MissionName);
            using (var db = new ZkDataContext())
            {
                db.Missions.Single(x => x.Name == missionName).MissionRunCount++;
                Account.AccountByName(db, request.Login).MissionRunCount++;
                db.SaveChanges();
            }

            return new NotifyMissionRunResponse();
        }

        async Task<RegisterResourceResponse> Process(RegisterResourceRequest request)
        {
            var ret = PlasmaServer.RegisterResource(request);
            return new RegisterResourceResponse() { ReturnValue = ret };
        }

        async Task<SubmitMissionScoreResponse> Process(SubmitMissionScoreRequest r)
        {
            var missionName = Mission.GetNameWithoutVersion(r.MissionName);

            using (var db = new ZkDataContext())
            {
                var acc = AuthServiceClient.VerifyAccountHashed(r.Login, r.PasswordHash);
                if (acc == null)
                {
                    Trace.TraceWarning("Invalid login attempt for {0}", r.Login);
                    System.Threading.Thread.Sleep(new Random().Next(2000));
                }

                acc.Xp += GlobalConst.XpForMissionOrBots;

                var mission = db.Missions.Single(x => x.Name == missionName);

                if (r.Score != 0 || mission.RequiredForMultiplayer)
                {
                    var scoreEntry = mission.MissionScores.FirstOrDefault(x => x.AccountID == acc.AccountID);
                    if (scoreEntry == null)
                    {
                        scoreEntry = new MissionScore() { MissionID = mission.MissionID, AccountID = acc.AccountID, Score = int.MinValue };
                        mission.MissionScores.Add(scoreEntry);
                    }

                    if (r.Score > scoreEntry.Score)
                    {
                        var max = mission.MissionScores.Max(x => (int?)x.Score);
                        if (max == null || max <= r.Score)
                        {
                            mission.TopScoreLine = r.Login;
                            acc.Xp += 150; // 150 for getting top score
                        }

                        scoreEntry.Score = r.Score;
                        scoreEntry.Time = DateTime.UtcNow;
                        scoreEntry.MissionRevision = mission.Revision;
                        scoreEntry.GameSeconds = r.GameSeconds;
                    }
                }

                acc.CheckLevelUp();
                db.SaveChanges();

                if (!acc.CanPlayMultiplayer)
                {
                    if (db.Missions.Where(x => x.RequiredForMultiplayer).All(y => y.MissionScores.Any(z => z.AccountID == acc.AccountID)))
                    {
                        acc.CanPlayMultiplayer = true;
                        db.SaveChanges();
                        Global.Server.PublishAccountUpdate(acc);
                        Global.Server.GhostPm(acc.Name, "Congratulations! You are now authorized to play MultiPlayer games!");
                    }
                }
            }

            return new SubmitMissionScoreResponse();
        }

        async Task<GetDefaultMissionsResponse> Process(GetDefaultMissionsRequest r)
        {
            using (var db = new ZkDataContext())
            {
                var ret = db.Missions.Where(x => x.FeaturedOrder != null && !x.IsDeleted).Select(x => new ClientMissionInfo()
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
                    DownloadHandle = x.Resources.Select(x1 => x1.InternalName).FirstOrDefault(),
                    OtherDependencies = x.ManualDependencies
                }).ToList(); // this does in single sql query

                // add image url
                foreach (var m in ret)
                {
                    m.ImageUrl = $"{GlobalConst.BaseSiteUrl}/img/missions/{m.MissionID}.png";
                }

                return new GetDefaultMissionsResponse() { Missions = ret };
                ;
            }
        }

        async Task<PublicCommunityInfo> Process(GetPublicCommunityInfo r)
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
                },
                3542);

            return info;
        }

        async Task<FeaturedCustomGameModesResponse> Process(GetFeaturedCustomGameModes r)
        {
            using (var db = new ZkDataContext())
            {
                var ret = db.GameModes.Where(x => x.IsFeatured).Select(x => new CustomGameModeInfo()
                {
                    DisplayName = x.DisplayName, FileContent = x.GameModeJson, FileName = x.ShortName
                }).ToList();

                return new FeaturedCustomGameModesResponse() { CustomGameModes = ret };
            }
        }

        async Task<SpringBattleInfo> Process(GetSpringBattleInfo r)
        {
            using (var db = new ZkDataContext())
            {
                var sb = db.SpringBattles.FirstOrDefault(x => x.EngineGameID == r.GameID);
                if (sb == null) return null;

                return new SpringBattleInfo()
                {
                    AutohostMode = sb.Mode, SpringBattleID = sb.SpringBattleID, IsMatchMaker = sb.IsMatchMaker, Title = sb.Title
                };
            }
        }
    }
}