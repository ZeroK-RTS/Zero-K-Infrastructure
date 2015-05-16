using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CampaignLib;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

namespace ZeroKLobby.MicroLobby.Campaign
{
    public class CampaignManager
    {
        static CampaignManager campaignManager;
        CampaignPage campaignPage;
        CampaignLib.Campaign currentCampaign;
        Dictionary<string, JournalPart> journalParts;
        CampaignSave currentCampaignSave;

        public CampaignManager(CampaignPage campaignPage)
        {
            this.campaignPage = campaignPage;
            campaignManager = this;
        }

        // todo
        public static void NotifyMissionCompletion()
        {
        }

        public void LoadCampaign(string campaignName)
        {
            String campaignDir = "./campaigns/" + campaignName + "/";
            string campaignJson = File.ReadAllText(campaignDir + "campaign.json");
            currentCampaign = JsonConvert.DeserializeObject<CampaignLib.Campaign>(campaignJson);

            string journalPartsJson = File.ReadAllText(campaignDir + "journalParts.json");
            journalParts = JsonConvert.DeserializeObject<Dictionary<string, JournalPart>>(journalPartsJson);

            currentCampaignSave = new CampaignSave("temp", currentCampaign.ID);

            Trace.TraceInformation("Loaded campaign {0}", currentCampaign.Name);
        }

        public void LoadCampaignSave(string campaignName, string saveName, bool isFullDir = false)
        {

            string saveDir = "";
            if (!isFullDir) saveDir = "./campaignsaves/" + campaignName + "/";
            string saveJson = File.ReadAllText(saveDir + saveName + (isFullDir ? "" : ".json"));
            currentCampaignSave = JsonConvert.DeserializeObject<CampaignSave>(saveJson);

            Trace.TraceInformation("Loaded save {0} for campaign {1}", currentCampaignSave.Name, currentCampaign.Name);
        }

        public static void PlayMission(Mission mission)
        {
            MarkMissionPlayed(mission.ID);
            ActionHandler.StartMission(mission.DownloadArchive);
        }

        public static void EnterMission(Mission mission)
        {
            campaignManager.campaignPage.EnterMission(mission);
        }

        public static List<Planet> GetUnlockedPlanets()
        {
            List<Planet> result = new List<Planet>();
            if (campaignManager == null) return result;

            foreach (KeyValuePair<string, Planet> planetEntry in campaignManager.currentCampaign.Planets)
            {
                if (IsPlanetUnlocked(planetEntry.Key)) result.Add(planetEntry.Value);
            }
            return result;
        }

        public static List<Planet> GetVisiblePlanets()
        {
            List<Planet> result = new List<Planet>();
            if (campaignManager == null) return result;
            foreach (KeyValuePair<string, Planet> planetEntry in campaignManager.currentCampaign.Planets)
            {
                if (IsPlanetVisible(planetEntry.Key)) result.Add(planetEntry.Value);
            }
            return result;
        }

        public static bool IsPlanetUnlocked(string planetID)
        {
            if (campaignManager == null) return false;

            if (!campaignManager.currentCampaign.Planets.ContainsKey(planetID)) return false;
            var planetMissions = campaignManager.currentCampaign.Planets[planetID].Missions;
            var missionProgress = campaignManager.currentCampaignSave.MissionProgress;
            foreach (Mission planetMission in planetMissions)
            {
                if (!missionProgress.ContainsKey(planetMission.ID)) continue;
                if (missionProgress[planetMission.ID].unlocked) return true;
            }
            return false;
        }

        public static bool IsPlanetVisible(string planetID)
        {
            if (campaignManager == null) return false;
            if (!campaignManager.currentCampaign.Planets.ContainsKey(planetID)) return false;
            if (!campaignManager.currentCampaign.Planets[planetID].HideIfLocked) return true;
            return IsPlanetUnlocked(planetID);
        }

        public static bool IsMissionUnlocked(Mission mission)
        {
            if (mission.StartUnlocked) return true;
            if (campaignManager == null) return false;
            //if (!campaignManager.currentCampaign.Planets.ContainsKey(planetID)) return false;
            //if (!campaignManager.currentCampaign.Planets[planetID].HideIfLocked) return true;
            
            var missionProgress = campaignManager.currentCampaignSave.MissionProgress;
            if (!missionProgress.ContainsKey(mission.ID)) return false;
            if (missionProgress[mission.ID].unlocked) return true;
            return false;
        }

        public static void UnlockMission(string missionID)
        {
            if (campaignManager == null) return;
            //if (!currentCampaign.Planets.ContainsKey(missionID)) throw new Exception("Planet " + missionID + " does not exist in campaign");

            if (campaignManager.currentCampaignSave.MissionProgress.ContainsKey(missionID))
            {
                campaignManager.currentCampaignSave.MissionProgress[missionID].unlocked = true;
            }
            else campaignManager.currentCampaignSave.MissionProgress.Add(missionID, new CampaignSave.MissionProgressData(missionID) { unlocked = true });
        }

        public static void CompleteMission(string missionID)
        {
            if (campaignManager == null) return;
            //if (!currentCampaign.Planets.ContainsKey(missionID)) throw new Exception("Planet " + missionID + " does not exist in campaign");

            if (campaignManager.currentCampaignSave.MissionProgress.ContainsKey(missionID))
            {
                //campaignManager.currentCampaignSave.MissionProgress[missionID].unlocked = true;
                campaignManager.currentCampaignSave.MissionProgress[missionID].completed = true;
            }
            else campaignManager.currentCampaignSave.MissionProgress.Add(missionID, new CampaignSave.MissionProgressData(missionID) { completed = true });
        }

        public static void MarkMissionPlayed(string missionID)
        {
            if (campaignManager == null) return;
            //if (!currentCampaign.Planets.ContainsKey(missionID)) throw new Exception("Planet " + missionID + " does not exist in campaign");

            if (campaignManager.currentCampaignSave.MissionProgress.ContainsKey(missionID))
            {
                campaignManager.currentCampaignSave.MissionProgress[missionID].played = true;
            }
            else campaignManager.currentCampaignSave.MissionProgress.Add(missionID, new CampaignSave.MissionProgressData(missionID) { played = true });
        }

        public static bool IsJournalRead(string journalID)
        {
            if (campaignManager == null) return false;

            if (!campaignManager.currentCampaign.Journals.ContainsKey(journalID)) return false;
            if (campaignManager.currentCampaignSave.JournalProgress[journalID] == null) return false;
            return campaignManager.currentCampaignSave.JournalProgress[journalID].read;
        }

        public static bool IsJournalUnlocked(string journalID)
        {
            if (campaignManager == null) return false;

            if (!campaignManager.currentCampaign.Journals.ContainsKey(journalID)) return false;
            if (campaignManager.currentCampaign.Journals[journalID].StartUnlocked) return true;
            if (!campaignManager.currentCampaignSave.JournalProgress.ContainsKey(journalID)) return false;
            return campaignManager.currentCampaignSave.JournalProgress[journalID].unlocked;
        }

        public static List<JournalViewEntry> GetVisibleJournals()
        {
            List<JournalViewEntry> ret = new List<JournalViewEntry>();
            if (campaignManager == null) return ret;

            foreach (var kvp in campaignManager.currentCampaign.Journals)
            {
                if (IsJournalUnlocked(kvp.Key))
                {
                    ret.Add(GetJournalViewEntry(kvp.Key));
                }
            }
            return ret;
        }

        public static JournalViewEntry GetJournalViewEntry(string journalID)
        {
            if (campaignManager == null) return null;

            string text = "";
            Journal journal = campaignManager.currentCampaign.Journals[journalID];
            if (campaignManager.currentCampaignSave.JournalProgress.ContainsKey(journalID))
                text = campaignManager.currentCampaignSave.JournalProgress[journalID].textSnapshot;
            if (String.IsNullOrEmpty(text))
                text = GetJournalTextSnapshot(journalID);
            JournalViewEntry entry = new JournalViewEntry(journalID, journal.Name, journal.Category, text);
            return entry;
        }

        public static string GetJournalTextSnapshot(string journalID)
        {
            if (campaignManager == null) return null;

            if (!campaignManager.currentCampaign.Journals.ContainsKey(journalID)) throw new Exception("Journal " + journalID + " does not exist in campaign");

            List<string> fragments = new List<string>();
            Journal journal = campaignManager.currentCampaign.Journals[journalID];
            foreach (string partID in journal.JournalPartIDs)
            {
                if (!campaignManager.journalParts.ContainsKey(partID)) throw new Exception("Journal part " + partID + " does not exist in campaign");
                var part = campaignManager.journalParts[partID];
                foreach (var requiredVar in part.VariablesRequired)
                {
                    // TODO: var check here to see if the fragment should be used
                }
                fragments.Add(part.Text);
            }
            return String.Concat(fragments);
        }

        public static void UnlockJournal(string journalID)
        {
            if (campaignManager == null) return;

            if (!campaignManager.currentCampaign.Journals.ContainsKey(journalID)) throw new Exception("Journal " + journalID + " does not exist in campaign");

            String textSnapshot = GetJournalTextSnapshot(journalID);
            if (campaignManager.currentCampaignSave.JournalProgress.ContainsKey(journalID))
            {
                campaignManager.currentCampaignSave.JournalProgress[journalID].unlocked = true;
                campaignManager.currentCampaignSave.JournalProgress[journalID].textSnapshot = textSnapshot;
            }
            else campaignManager.currentCampaignSave.JournalProgress.Add(journalID, new CampaignSave.JournalProgressData(journalID) { unlocked = true, textSnapshot = textSnapshot });
        }

        public CampaignLib.Campaign GetCampaign()
        {
            return currentCampaign;
        }

        public CampaignSave GetSave()
        {
            return currentCampaignSave;
        }

        public class JournalViewEntry
        {
            public string id;
            public string name;
            public string category;
            public string text;
            public string image;

            public JournalViewEntry(string id, string name, string category, string text, string image = null)
            {
                this.id = id;
                this.name = name;
                this.category = category;
                this.text = text;
                this.image = image;
            }
        }
    }
}
