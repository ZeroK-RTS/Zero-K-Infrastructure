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
    class CampaignManager
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

        public void LoadCampaignSave(string campaignName, string saveName)
        {
            string saveDir = "./campaignsaves/" + campaignName + "/";
            string saveJson = File.ReadAllText(saveDir + saveName + ".json");
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
            if (!currentCampaignSave.PlanetProgress.ContainsKey(planetID)) return false;
            return currentCampaignSave.PlanetProgress[planetID].unlocked;
        }

        public bool IsPlanetVisible(string planetID)
        {
            if (!currentCampaign.Planets.ContainsKey(planetID)) return false;
            if (!currentCampaignSave.PlanetProgress.ContainsKey(planetID)) return false;
            if (!currentCampaign.Planets[planetID].HideIfLocked) return true;
            return currentCampaignSave.PlanetProgress[planetID].unlocked;
        }

        public CampaignLib.Campaign GetCampaign()
        {
            return currentCampaign;
        }
    }
}
