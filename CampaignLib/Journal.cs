using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    public class Journal
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Icon { get; set; }
        public string Category { get; set; }
        public string PlanetID { get; set; }
        public List<String> JournalPartIDs { get; set; }

        public Journal(string id)
        {
            ID = id;
            JournalPartIDs = new List<String>();
        }

        public string GetJournalText()
        {
            List<string> selected = new List<string>();
            /*
            foreach (JournalPart part in JournalParts)
            {
                if (part.Accept()) selected.Add(part.Text);
            }
            */
            return String.Concat(selected);
        }

        //public List<Planet> Planets { get; set; }
    }
}
