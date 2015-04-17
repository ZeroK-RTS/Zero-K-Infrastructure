using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CampaignLib.Tester
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateCampaignSample();
            Campaign campaign = ReadCampaign();
            Dictionary<string, JournalPart> journalParts = readJournalParts();

            string journal1Part1 = campaign.Journals["journal1"].JournalPartIDs[0];
            string journal1Text = journalParts[journal1Part1].Text;
            Console.WriteLine(journal1Text);

            WriteCampaignProgress(campaign);
        }

        static void WriteCampaignProgress(Campaign campaign)
        {
            CampaignProgress progress = new CampaignProgress(campaign.ID);
            var progress1 = new CampaignProgress.JournalProgressData("journal1") { unlocked = true };
            progress1.textSnapshot = campaign.Journals["journal1"].GetJournalText();
            progress.JournalProgress.Add("journal1", new CampaignProgress.JournalProgressData("journal1") { unlocked = true });

            var progress2 = new CampaignProgress.PlanetProgressData("planet1");
            progress2.unlocked = true;
            progress.PlanetProgress.Add("planet1", progress2);

            string progressJson = JsonConvert.SerializeObject(progress, Formatting.Indented);
            File.WriteAllText("test_progress.json", progressJson);
        }

        static Dictionary<string, JournalPart> readJournalParts()
        {
            string journalPartsJson = File.ReadAllText("test_journalParts.json");
            return JsonConvert.DeserializeObject<Dictionary<string, JournalPart>>(journalPartsJson);
        }

        static Campaign ReadCampaign()
        {
            string campaignJson = File.ReadAllText("test.json");
            Campaign loadedCampaign = JsonConvert.DeserializeObject<Campaign>(campaignJson);

            Console.WriteLine("Loaded campaign {0}", loadedCampaign.Name);

            return loadedCampaign;
        }

        static void CreateCampaignSample()
        {
            Campaign newCampaign = new Campaign("testCampaign") { Name = "Test Campaign" };
            Planet planet1 = new Planet("planet1") { Name = "Licho", HideIfLocked = false };
            Planet planet2 = new Planet("planet2") { Name = "Saktoth", HideIfLocked = false };

            planet1.LinkedPlanets.Add(planet2.ID);

            newCampaign.Planets.Add(planet1.ID, planet1);
            newCampaign.Planets.Add(planet2.ID, planet2);

            Journal journal = new Journal("journal1") { Name = "Entry 1", PlanetID = planet1.ID };
            journal.JournalPartIDs.Add("journal1_1");
            newCampaign.Journals.Add(journal.ID, journal);

            string campaignDefJson = JsonConvert.SerializeObject(newCampaign, Formatting.Indented);
            File.WriteAllText("test.json", campaignDefJson);

            var journalParts = new Dictionary<string, JournalPart>();
            journalParts.Add("journal1_1", new JournalPart("journal1_1") { Text = "The quick brown fox jumps over the lazy dog", Name = "Entry 1 part 1" });

            string journalPartsJson = JsonConvert.SerializeObject(journalParts, Formatting.Indented);
            File.WriteAllText("test_journalParts.json", journalPartsJson);
        }
    }
}
