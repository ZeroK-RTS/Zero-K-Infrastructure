using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    public class Campaign
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Blurb { get; set; }
        public string Background { get; set; }
        public string Icon { get; set; }
        public Dictionary<string, Planet> Planets { get; set; }
        public Dictionary<string, Journal> Journals { get; set; }

        public Campaign(string id)
        {
            ID = id;
            Planets = new Dictionary<string,Planet>();
            Journals = new Dictionary<string,Journal>();
        }
    }
}
