using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Web.Services;
using ZkData;

namespace PlasmaShared
{
    public class DownloadFileResult
    {
        public List<string> links;
        public byte[] torrent;
        public List<string> dependencies;
        public ResourceType resourceType;
        public string torrentFileName;
    }

    [ServiceContract]
    public interface IContentService
    {
        [OperationContract]
        DownloadFileResult DownloadFile(string internalName);

        [OperationContract]
        List<ResourceData> FindResourceData(string[] words, ResourceType? type = null);

        [OperationContract]
        List<string> GetEloTop10();

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
        bool VerifyAccountData(string login, string password);

        [OperationContract]
        AccountInfo GetAccountInfo(string login, string password);
    }
}
