using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using LobbyClient;
using Microsoft.Linq.Translations;
using PlasmaDownloader;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb
{
    [ServiceContract]
    [Obsolete("Use ContentServiceClient instead")]
    public interface IContentService
    {
        [OperationContract]
        DownloadFileResponse DownloadFile(string internalName);


        [OperationContract]
        List<string> GetEngineList(string platform);

        [OperationContract]
        string GetDefaultEngine();


        [OperationContract]
        List<ResourceData> FindResourceData(string[] words, ResourceType? type = null);

        /// <summary>
        /// Finds resource by either md5 or internal name
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="internalName"></param>
        /// <returns></returns>
        [OperationContract]
        ResourceData GetResourceData(string md5, string internalName);

        [OperationContract]
        List<ResourceData> GetResourceList(DateTime? lastChange, out DateTime currentTime);

        [OperationContract]
        ScriptMissionData GetScriptMissionData(string name);

        [OperationContract(IsOneWay = true)]
        void NotifyMissionRun(string login, string missionName);

        [OperationContract]
        ReturnValue RegisterResource(int apiVersion,
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
                                                                  byte[] torrentData);

        [OperationContract]
        void SubmitMissionScore(string login, string passwordHash, string missionName, int score, int gameSeconds, string missionVars = "");

        [OperationContract]
        List<ClientMissionInfo> GetDefaultMissions();

        [OperationContract]
        PublicCommunityInfo GetPublicCommunityInfo();
        
        [OperationContract]
        List<CustomGameModeInfo> GetFeaturedCustomGameModes();


        [OperationContract]
        SpringBattleInfo GetSpringBattleInfo(string gameid);
    }

    
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "ContentService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select ContentService.svc or ContentService.svc.cs at the Solution Explorer and start debugging.
    [Obsolete("Use ContenServiceClient instead!")]
    public class ContentService : IContentService
    {
        
        public DownloadFileResponse DownloadFile(string internalName)
        {
            return GlobalConst.GetContentService().Query(new DownloadFileRequest() { InternalName = internalName });
        }

        public List<string> GetEngineList(string platform)
        {
            return GlobalConst.GetContentService().Query(new GetEngineListRequest() { Platform = platform }).Engines;
        }

        public string GetDefaultEngine()
        {
            return GlobalConst.GetContentService().Query(new GetDefaultEngineRequest()).DefaultEngine;
        }


        public List<ResourceData> FindResourceData(string[] words, ResourceType? type = null)
        {
            return GlobalConst.GetContentService().Query(new FindResourceDataRequest() { Words = words, Type = type }).Resources;
        }
        
        public ResourceData GetResourceData(string md5, string internalName)
        {
            return GlobalConst.GetContentService().Query(new GetResourceDataRequest() { Md5 = md5, InternalName = internalName });
        }

        
        public List<ResourceData> GetResourceList(DateTime? lastChange, out DateTime currentTime)
        {
            return PlasmaServer.GetResourceList(lastChange, out currentTime);
        }

        public ScriptMissionData GetScriptMissionData(string name)
        {
            return GlobalConst.GetContentService().Query(new GetScriptMissionDataRequest() { MissionName = name });
        }


        
        public void NotifyMissionRun(string login, string missionName)
        {
            GlobalConst.GetContentService().Query(new NotifyMissionRun() { Login = login, MissionName = missionName });
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
            return GlobalConst.GetContentService().Query(new RegisterResourceRequest(apiVersion, springVersion, md5, length, resourceType, archiveName, internalName, serializedData, dependencies, minimap, metalMap, heightMap, torrentData)).ReturnValue;
        }

        
        public void SubmitMissionScore(string login, string passwordHash, string missionName, int score, int gameSeconds, string missionVars = "")
        {
            GlobalConst.GetContentService().Query(new SubmitMissionScoreRequest() { Login = login, PasswordHash = passwordHash, MissionName = missionName, Score = score, GameSeconds = gameSeconds, MissionVars = missionVars });
        }

        public List<ClientMissionInfo> GetDefaultMissions()
        {
            return GlobalConst.GetContentService().Query(new GetDefaultMissionsRequest()).Missions;
        }

        public PublicCommunityInfo GetPublicCommunityInfo()
        {
            return GlobalConst.GetContentService().Query(new GetPublicCommunityInfo());
        }

        public List<CustomGameModeInfo> GetFeaturedCustomGameModes()
        {
            return GlobalConst.GetContentService().Query(new GetFeaturedCustomGameModes()).CustomGameModes;
        }

        public SpringBattleInfo GetSpringBattleInfo(string gameid)
        {
            return GlobalConst.GetContentService().Query(new GetSpringBattleInfo() { GameID = gameid });
        }
    }


}
