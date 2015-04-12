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
            WriteCampaignProgress(campaign);
        }

        static void WriteCampaignProgress(Campaign campaign)
        {
            CampaignProgress progress = new CampaignProgress(campaign.ID);
            var progress1 = new CampaignProgress.JournalProgressData("journal1") { unlocked = true };
            progress1.textSnapshot = campaign.Journals.First(x => x.ID == "journal1").GetJournalText();
            progress.JournalProgress.Add("journal1", new CampaignProgress.JournalProgressData("journal1") { unlocked = true });

            var progress2 = new CampaignProgress.PlanetProgressData("planet1");
            progress2.unlocked = true;
            progress.PlanetProgress.Add("planet1", progress2);

            string progressJson = JsonConvert.SerializeObject(progress, Formatting.Indented);
            File.WriteAllText("test_progress.json", progressJson);
        }

        static Campaign ReadCampaign()
        {
            Stream stream = new FileStream("test.json", FileMode.Open);
            var serializer = new DataContractJsonSerializer(typeof(Campaign));
            Campaign loadedCampaign = (Campaign)serializer.ReadObject(stream);

            Console.WriteLine("Loaded campaign {0}", loadedCampaign.Name);

            return loadedCampaign;
        }

        static void CreateCampaignSample()
        {
            Campaign newCampaign = new Campaign("testCampaign") { Name = "Test Campaign" };
            Planet planet1 = new Planet("planet1") { Name = "Licho", HideIfLocked = false };
            Planet planet2 = new Planet("planet2") { Name = "Saktoth", HideIfLocked = false };

            planet1.LinkedPlanets.Add(planet2.ID);

            newCampaign.Planets.Add(planet1);
            newCampaign.Planets.Add(planet2);

            Journal journal = new Journal("journal1") { Name = "Entry 1", PlanetID = planet1.ID };
            journal.JournalParts.Add(new JournalPart("journal1_1") { Text = "The quick brown fox jumps over the lazy dog", Name = "Entry 1 part 1" });
            newCampaign.Journals.Add(journal);

            string campaignDefJson = JsonConvert.SerializeObject(newCampaign, Formatting.Indented);
            File.WriteAllText("test.json", campaignDefJson);
        }
    }
}
