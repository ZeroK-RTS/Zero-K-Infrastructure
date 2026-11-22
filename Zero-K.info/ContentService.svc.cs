using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using LobbyClient;
using PlasmaDownloader;
using PlasmaShared;
using ZkData;

namespace PlasmaShared
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
}

namespace ZeroKWeb
{
    /// <summary>
    /// The WCF endpoint, which ContentService.svc points at. **Deprecated**: server-side WCF has
    /// no successor on .NET 9, and /ContentService carries the same operations in JSON.
    ///
    /// Nothing in this repository calls it. Every caller here uses IContentServiceClient, and
    /// inside the website Global.cs overrides that to run the implementation in-process - which
    /// is also why these bodies are one line each. It is hosted purely for clients deployed
    /// before the JSON endpoint existed, and this repository cannot see them.
    ///
    /// **So it says who is still calling it.** HOSTING.md used to record that retiring this
    /// needed production access logs; it does not any more. Every operation reports the caller's
    /// user agent and address, which is what a deployed client can be recognised by, plus the
    /// login and the API version where the operation happens to carry one.
    ///
    /// It reports *counts*, not calls, and the difference matters: ZkServerTraceListener turns
    /// every trace into its own ZkDataContext and one LogEntries insert. MissionService.svc can
    /// log per call because publishing a mission is rare; DownloadFile and GetResourceData are
    /// whatever a fleet of outdated lobbies asks for, so logging each one would put a database
    /// write on a path that currently has none. See LogLegacyCall.
    ///
    /// Note aspNetCompatibilityEnabled is false in Web.config, so HttpContext.Current is null
    /// here - the caller can only be identified through OperationContext, and that is what
    /// LogLegacyCall does.
    /// </summary>
    [Obsolete("Use ContenServiceClient instead!")]
    public class ContentService : IContentService
    {
        
        public DownloadFileResponse DownloadFile(string internalName)
        {
            LogLegacyCall("DownloadFile");
            return GlobalConst.GetContentService().Query(new DownloadFileRequest() { InternalName = internalName });
        }

        public List<string> GetEngineList(string platform)
        {
            LogLegacyCall("GetEngineList");
            return GlobalConst.GetContentService().Query(new GetEngineListRequest() { Platform = platform }).Engines;
        }

        public string GetDefaultEngine()
        {
            LogLegacyCall("GetDefaultEngine");
            return GlobalConst.GetContentService().Query(new GetDefaultEngineRequest()).DefaultEngine;
        }


        public List<ResourceData> FindResourceData(string[] words, ResourceType? type = null)
        {
            LogLegacyCall("FindResourceData");
            return GlobalConst.GetContentService().Query(new FindResourceDataRequest() { Words = words, Type = type }).Resources;
        }
        
        public ResourceData GetResourceData(string md5, string internalName)
        {
            LogLegacyCall("GetResourceData");
            return GlobalConst.GetContentService().Query(new GetResourceDataRequest() { Md5 = md5, InternalName = internalName });
        }

        
        public List<ResourceData> GetResourceList(DateTime? lastChange, out DateTime currentTime)
        {
            LogLegacyCall("GetResourceList");
            return PlasmaServer.GetResourceList(lastChange, out currentTime);
        }

        public ScriptMissionData GetScriptMissionData(string name)
        {
            LogLegacyCall("GetScriptMissionData");
            return GlobalConst.GetContentService().Query(new GetScriptMissionDataRequest() { MissionName = name });
        }


        
        public void NotifyMissionRun(string login, string missionName)
        {
            LogLegacyCall("NotifyMissionRun", login);
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
            // apiVersion and springVersion are what a stuck old client is recognised by.
            LogLegacyCall("RegisterResource", null, "api version " + apiVersion + ", spring " + springVersion);
            return GlobalConst.GetContentService().Query(new RegisterResourceRequest(apiVersion, springVersion, md5, length, resourceType, archiveName, internalName, serializedData, dependencies, minimap, metalMap, heightMap, torrentData)).ReturnValue;
        }

        
        public void SubmitMissionScore(string login, string passwordHash, string missionName, int score, int gameSeconds, string missionVars = "")
        {
            LogLegacyCall("SubmitMissionScore", login);
            GlobalConst.GetContentService().Query(new SubmitMissionScoreRequest() { Login = login, PasswordHash = passwordHash, MissionName = missionName, Score = score, GameSeconds = gameSeconds, MissionVars = missionVars });
        }

        public List<ClientMissionInfo> GetDefaultMissions()
        {
            LogLegacyCall("GetDefaultMissions");
            return GlobalConst.GetContentService().Query(new GetDefaultMissionsRequest()).Missions;
        }

        public PublicCommunityInfo GetPublicCommunityInfo()
        {
            LogLegacyCall("GetPublicCommunityInfo");
            return GlobalConst.GetContentService().Query(new GetPublicCommunityInfo());
        }

        public List<CustomGameModeInfo> GetFeaturedCustomGameModes()
        {
            LogLegacyCall("GetFeaturedCustomGameModes");
            return GlobalConst.GetContentService().Query(new GetFeaturedCustomGameModes()).CustomGameModes;
        }

        public SpringBattleInfo GetSpringBattleInfo(string gameid)
        {
            LogLegacyCall("GetSpringBattleInfo");
            return GlobalConst.GetContentService().Query(new GetSpringBattleInfo() { GameID = gameid });
        }

        /// <summary>
        /// Goes to LogEntries, which Admin/TraceLogs shows and which keeps 14 days - long enough
        /// to answer whether anyone is still on the WCF path, short enough not to accumulate.
        ///
        /// The counting, and the reason for it, are in LegacyCallReporter; Tests.Portable drives
        /// that. What is here is the half that only WCF can supply: aspNetCompatibilityEnabled is
        /// false in Web.config, so HttpContext.Current is null and the caller has to be read off
        /// OperationContext.
        ///
        /// It cannot throw. This is bookkeeping attached to a live endpoint, and an endpoint that
        /// started failing because of its own deprecation notice would be worse than the WCF it
        /// is here to retire. IncomingMessageProperties' indexer throws when a key is absent,
        /// which is why every read goes through TryGetValue.
        /// </summary>
        static void LogLegacyCall(string operation, string caller = null, string detail = null)
        {
            try
            {
                string client = null, remote = null;
                var incoming = OperationContext.Current?.IncomingMessageProperties;
                if (incoming != null)
                {
                    object http;
                    if (incoming.TryGetValue(HttpRequestMessageProperty.Name, out http))
                        client = (http as HttpRequestMessageProperty)?.Headers["User-Agent"];
                    object endpoint;
                    if (incoming.TryGetValue(RemoteEndpointMessageProperty.Name, out endpoint))
                        remote = (endpoint as RemoteEndpointMessageProperty)?.Address;
                }

                var line = legacyCalls.Record(operation, client, caller, remote, detail);
                if (line != null) Trace.TraceInformation(line);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("ContentService.svc: could not record a legacy call: {0}", ex.Message);
            }
        }

        static readonly LegacyCallReporter legacyCalls =
            new LegacyCallReporter("ContentService.svc", TimeSpan.FromHours(1), 200);
    }


}
