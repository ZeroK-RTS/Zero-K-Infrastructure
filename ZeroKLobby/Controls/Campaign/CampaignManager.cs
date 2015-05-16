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
        CampaignLib.Campaign currentCampaign;
        Dictionary<string, JournalPart> journalParts;
        CampaignSave currentCampaignSave;

        public void LoadCampaign(string campaignName)
        {
            String campaignDir = "./campaigns/" + campaignName + "/";
            string campaignJson = File.ReadAllText(campaignDir + "campaign.json");
            currentCampaign = JsonConvert.DeserializeObject<CampaignLib.Campaign>(campaignJson);

            string journalPartsJson = File.ReadAllText(campaignDir + "journalParts.json");
            journalParts = JsonConvert.DeserializeObject<Dictionary<string, JournalPart>>(journalPartsJson);

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

        public List<Planet> GetUnlockedPlanets()
        {
            List<Planet> result = new List<Planet>();
            foreach (KeyValuePair<string, Planet> planetEntry in currentCampaign.Planets)
            {
                if (IsPlanetUnlocked(planetEntry.Key)) result.Add(planetEntry.Value);
            }
            return result;
        }

        public List<Planet> GetVisiblePlanets()
        {
            List<Planet> result = new List<Planet>();
            foreach (KeyValuePair<string, Planet> planetEntry in currentCampaign.Planets)
            {
                if (IsPlanetVisible(planetEntry.Key)) result.Add(planetEntry.Value);
            }
            return result;
        }

        public bool IsPlanetUnlocked(string planetID)
        {
            if (!currentCampaign.Planets.ContainsKey(planetID)) return false;
            var planetMissions = currentCampaign.Planets[planetID].Missions;
            var missionProgress = currentCampaignSave.MissionProgress;
            foreach (Mission planetMission in planetMissions)
            {
                if (!missionProgress.ContainsKey(planetMission.ID)) continue;
                if (missionProgress[planetMission.ID].unlocked) return true;
            }
            return false;
        }

        public bool IsPlanetVisible(string planetID)
        {
            if (!currentCampaign.Planets.ContainsKey(planetID)) return false;
            if (!currentCampaign.Planets[planetID].HideIfLocked) return true;
            return IsPlanetUnlocked(planetID);
        }

        public bool IsJournalRead(string journalID)
        {
            if (!currentCampaign.Journals.ContainsKey(journalID)) return false;
            if (currentCampaignSave.JournalProgress[journalID] == null) return false;
            return currentCampaignSave.JournalProgress[journalID].read;
        }

        public bool IsJournalUnlocked(string journalID)
        {
            if (!currentCampaign.Journals.ContainsKey(journalID)) return false;
            if (currentCampaign.Journals[journalID].StartUnlocked) return true;
            if (!currentCampaignSave.JournalProgress.ContainsKey(journalID)) return false;
            return currentCampaignSave.JournalProgress[journalID].unlocked;
        }

        public List<JournalViewEntry> GetVisibleJournals()
        {
            List<JournalViewEntry> ret = new List<JournalViewEntry>();
            foreach (var kvp in currentCampaign.Journals)
            {
                if (IsJournalUnlocked(kvp.Key))
                {
                    ret.Add(GetJournalViewEntry(kvp.Key));
                }
            }
            return ret;
        }

        public JournalViewEntry GetJournalViewEntry(string journalID)
        {
            string text = "";
            Journal journal = currentCampaign.Journals[journalID];
            if (currentCampaignSave.JournalProgress.ContainsKey(journalID))
                text = currentCampaignSave.JournalProgress[journalID].textSnapshot;
            if (String.IsNullOrEmpty(text))
                text = GetJournalTextSnapshot(journalID);
            JournalViewEntry entry = new JournalViewEntry(journalID, journal.Name, journal.Category, text);
            return entry;
        }

        public string GetJournalTextSnapshot(string journalID)
        {
            if (!currentCampaign.Journals.ContainsKey(journalID)) throw new Exception("Journal " + journalID + " does not exist in campaign");

            List<string> fragments = new List<string>();
            Journal journal = currentCampaign.Journals[journalID];
            foreach (string partID in journal.JournalPartIDs)
            {
                if (!journalParts.ContainsKey(partID)) throw new Exception("Journal part " + partID + " does not exist in campaign");
                var part = journalParts[partID];
                foreach (var requiredVar in part.VariablesRequired)
                {
                    // TODO: var check here to see if the fragment should be used
                }
                fragments.Add(part.Text);
            }
            return String.Concat(fragments);
        }

        public void UnlockJournal(string journalID)
        {
            if (!currentCampaign.Journals.ContainsKey(journalID)) throw new Exception("Journal " + journalID + " does not exist in campaign");

            String textSnapshot = GetJournalTextSnapshot(journalID);
            if (currentCampaignSave.JournalProgress.ContainsKey(journalID))
            {
                currentCampaignSave.JournalProgress[journalID].unlocked = true;
                currentCampaignSave.JournalProgress[journalID].textSnapshot = textSnapshot;
            }
            else currentCampaignSave.JournalProgress.Add(journalID, new CampaignSave.JournalProgressData(journalID) { unlocked = true, textSnapshot = textSnapshot });
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
