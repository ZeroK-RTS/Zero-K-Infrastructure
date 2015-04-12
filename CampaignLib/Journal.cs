using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    [DataContract]
    public class Journal
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public string Icon { get; set; }
        [DataMember]
        public string Category { get; set; }
        [DataMember]
        public Planet Planet { get; set; }
        [DataMember]
        public List<JournalPart> JournalParts { get; set; }

        public Journal(string id)
        {
            ID = id;
            JournalParts = new List<JournalPart>();
        }

        public string GetJournalText()
        {
            List<string> selected = new List<string>();
            foreach (JournalPart part in JournalParts)
            {
                if (part.Accept()) selected.Add(part.Text);
            }
            return String.Concat(selected);
        }

        //public List<Planet> Planets { get; set; }
    }
}
