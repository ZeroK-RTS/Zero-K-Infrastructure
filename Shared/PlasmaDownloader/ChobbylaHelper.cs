using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData;

namespace PlasmaDownloader
{
    public interface IChobbylaProgress
    {
        Download Download { get; set; }
        string Status { get; set; }
    }


    public static class ChobbylaHelper
    {
        public static Task<bool> DownloadFile(this PlasmaDownloader downloader, DownloadType type,
            string name,
            IChobbylaProgress progress) => DownloadFile(downloader, name, type, name, progress);

        public static async Task<bool> DownloadFile(this PlasmaDownloader downloader,
            string desc,
            DownloadType type,
            string name,
            IChobbylaProgress progress, int retries = 0)
        {
            Download down;
            do
            {
                down = downloader.GetResource(type, name);

                if (progress != null)
                {
                    progress.Status = desc;
                    progress.Download = down;
                }

                var dlTask = down?.WaitHandle.AsTask(TimeSpan.FromMinutes(30));
                if (dlTask != null) await dlTask.ConfigureAwait(false);
            } while (down?.IsAborted != true && down?.IsComplete != true && retries-- > 0);

            if (down?.IsComplete == false)
            {
                if (progress != null) progress.Status = $"Download of {progress.Download.Name} has failed";
                return false;
            }
            return true;
        }

        public static bool DownloadUrl(this PlasmaDownloader downloader,
            string desc,
            string url,
            string filePathTarget,
            IChobbylaProgress progress)
        {
            progress.Status = desc;
            var wfd = new WebClient();
            wfd.DownloadFile(url, filePathTarget);
            return true;
        }


        public static bool UpdatePublicCommunityInfo(this PlasmaDownloader downloader, IChobbylaProgress progress)
        {
            try
            {
                progress.Status = "Loading community news";
                var folder = Path.Combine(downloader.SpringPaths.WritableDirectory, "news");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                var info = GlobalConst.GetContentService().Query(new GetPublicCommunityInfo());
                File.WriteAllText(Path.Combine(folder, "community.json"), JsonConvert.SerializeObject(info));
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Loading public community info failed: {0}", ex);
                progress.Status = "Loading community news failed";
                return false;
            }
        }
        
        public static bool UpdateFeaturedCustomGameModes(this PlasmaDownloader downloader, IChobbylaProgress progress)
        {
            try
            {
                progress.Status = "Loading custom game modes";
                var folder = Path.Combine(downloader.SpringPaths.WritableDirectory, "CustomModes");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                var modes = GlobalConst.GetContentService().Query(new GetFeaturedCustomGameModes()).CustomGameModes;
                foreach (var mode in modes)
                {    
                    File.WriteAllText(Path.Combine(folder, $"{mode.FileName}.json"), mode.FileContent);    
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Loading custom game modes failed: {0}", ex);
                progress.Status = "Loading custom game modes failed";
                return false;
            }
        }
        


        public static async Task<bool> UpdateMissions(this PlasmaDownloader downloader, IChobbylaProgress progress)
        {
            try
            {
                progress.Status = "Downloading missions";
                var missions = GlobalConst.GetContentService().Query(new GetDefaultMissionsRequest()).Missions;

                var missionsFolder = Path.Combine(downloader.SpringPaths.WritableDirectory, "missions");
                if (!Directory.Exists(missionsFolder)) Directory.CreateDirectory(missionsFolder);
                var missionFile = Path.Combine(missionsFolder, "missions.json");

                List<ClientMissionInfo> existing = null;
                if (File.Exists(missionFile))
                    try
                    {
                        existing = JsonConvert.DeserializeObject<List<ClientMissionInfo>>(File.ReadAllText(missionFile));
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Error reading mission file {0} : {1}", missionFile, ex);
                    }
                existing = existing ?? new List<ClientMissionInfo>();

                var toDownload =
                    missions.Where(
                            m => !existing.Any(x => (x.MissionID == m.MissionID) && (x.Revision == m.Revision) && (x.DownloadHandle == m.DownloadHandle)))
                        .ToList();

                // download mission files
                foreach (var m in toDownload)
                {
                    if (m.IsScriptMission && (m.Script != null)) m.Script = m.Script.Replace("%MAP%", m.Map);
                    if (!m.IsScriptMission) if (!await downloader.DownloadFile("Downloading mission " + m.DisplayName, DownloadType.MISSION, m.DownloadHandle, progress).ConfigureAwait(false)) return false;
                    if (!downloader.DownloadUrl("Downloading image", m.ImageUrl, Path.Combine(missionsFolder, $"{m.MissionID}.png"), progress)) return false;
                }

                File.WriteAllText(missionFile, JsonConvert.SerializeObject(missions));

                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error updating missions: {0}", ex);
                return false;
            }
        }
    }
}