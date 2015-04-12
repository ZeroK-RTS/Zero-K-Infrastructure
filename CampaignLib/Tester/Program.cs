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
            //CreateCampaignSample();
            ReadCampaign();
        }

        static void ReadCampaign()
        {
            Stream stream = new FileStream("test.json", FileMode.Open);
            var serializer = new DataContractJsonSerializer(typeof(Campaign));
            Campaign loadedCampaign = (Campaign)serializer.ReadObject(stream);

            Console.WriteLine("Loaded campaign {0}", loadedCampaign.Name);
        }

        static void CreateCampaignSample()
        {
            Campaign newCampaign = new Campaign("testCampaign") { Name = "Test Campaign" };
            newCampaign.Planets.Add(new Planet("planet1") { Name = "Licho", HideIfLocked = false });
            newCampaign.Planets.Add(new Planet("planet2") { Name = "Saktoth", HideIfLocked = false });

            Journal journal = new Journal("journal1") { Name = "Entry 1" };
            journal.JournalParts.Add(new JournalPart("journal1_1") { Text = "The quick brown fox jumps over the lazy dog", Name = "Entry 1 part 1" });
            newCampaign.Journals.Add(journal);

            Stream stream = new FileStream("test.json", FileMode.Create);
            var serializer = new DataContractJsonSerializer(typeof(Campaign));
            serializer.WriteObject(stream, newCampaign);

            stream.Position = 0;
            StreamReader sr = new StreamReader(stream);
            Console.WriteLine("Writing campaign as JSON:");
            Console.WriteLine(sr.ReadToEnd());
            stream.Close();
        }
    }
}
