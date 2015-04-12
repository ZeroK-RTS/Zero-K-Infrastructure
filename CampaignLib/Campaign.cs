using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    [DataContract]
    public class Campaign
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public string Blurb { get; set; }
        [DataMember]
        public string Background { get; set; }
        [DataMember]
        public string Icon { get; set; }
        [DataMember]
        public List<Planet> Planets { get; set; }
        [DataMember]
        public List<Journal> Journals { get; set; }

        public Campaign(string id)
        {
            ID = id;
            Planets = new List<Planet>();
            Journals = new List<Journal>();
        }
    }
}
