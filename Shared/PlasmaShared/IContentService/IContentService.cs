using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Web.Services;
using ZkData;

namespace PlasmaShared
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class ApiMessageAttribute : Attribute {}

    
    [ApiMessage]
    public abstract class ApiRequest<T> where T : ApiResponse
    {
    }
    
    [ApiMessage]
    public abstract class ApiResponse {}


    public class DownloadFileRequest: ApiRequest<DownloadFileResponse> {
        public string InternalName;
    }

    public class DownloadFileResponse: ApiResponse
    {
        public List<string> links;
        public byte[] torrent;
        public List<string> dependencies;
        public ResourceType resourceType;
        public string torrentFileName;
    }

    public class GetEngineListRequest: ApiRequest<GetEngineListResponse>
    {
        public string Platform;
    }

    public class GetEngineListResponse: ApiResponse
    {
        public List<string> Engines;
    }

    public class GetDefaultEngineRequest: ApiRequest<GetDefaultEngineResponse> { }

    public class GetDefaultEngineResponse: ApiResponse
    {
        public string DefaultEngine;
    }
    
    public class FindResourceDataRequest:ApiRequest<FindResourceDataResponse>
    {
        public string[] Words;
        public ResourceType? Type;
    }

    public class FindResourceDataResponse: ApiResponse
    {
        public List<ResourceData> Resources;
    }
    
    /// <summary>
    /// Finds resource by either md5 or internal name
    /// </summary>
    public class GetResourceDataRequest:ApiRequest<ResourceData>
    {
        public string Md5;
        public string InternalName;
    }


    public class NewsItem
    {
        public string Header { get; set; }
        public string Text { get; set; }
        public DateTime? Time { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
    }


    public class LadderItem
    {
        public string Name { get; set; }
        public string Clan { get; set; } 
        public string Icon { get; set; }
        public int Level { get; set; }
        public string Country { get; set; }
        public bool IsAdmin { get; set; }
        public int AccountID { get; set; }
    }

    public class ForumItem
    {
        public int ThreadID { get; set; }
        public string Header { get; set; }
        public string Url { get; set; }
        public DateTime Time { get; set; }
        public bool IsRead { get; set; }
    }

    public class MapItem
    {
        public int ResourceID { get; set; }
        public string Name { get; set; }
        public MapSupportLevel SupportLevel { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public bool? IsAssymetrical { get; set; }
        public int? Hills { get; set; }
        public int? WaterLevel { get; set; }
        public bool? Is1v1 { get; set; }
        public bool? IsTeams { get; set; }
        public bool? IsFFA { get; set; }
        public bool? IsChickens { get; set; }
        public int? FFAMaxTeams { get; set; }
        public int? RatingCount { get; set; }
        public int? RatingSum { get; set; }
        public bool? IsSpecial { get; set; }
    }

    
    public class PublicCommunityInfo
    {
        public List<NewsItem> NewsItems { get; set; } = new List<NewsItem>();
        public List<LadderItem> LadderItems { get; set; } = new List<LadderItem>();
        public List<ForumItem> ForumItems { get; set; } = new List<ForumItem>();
        public List<MapItem> MapItems { get; set; } = new List<MapItem>();
        public bool UserCountLimited { get; set; }
    }

    public class SpringBattleInfo
    {
        public int SpringBattleID { get; set; }
        public AutohostMode AutohostMode { get; set; }
        public bool IsMatchMaker { get; set; }
        public string Title { get; set; }
    }

    public class CustomGameModeInfo
    {
        public string DisplayName { get; set; }
        public string FileName { get; set; }
        
        public string FileContent { get; set; }
    }


    [ServiceContract]
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
        ResourceData GetResourceDataByInternalName(string internalName);

        [OperationContract]
        ResourceData GetResourceDataByResourceID(int resourceID);

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
