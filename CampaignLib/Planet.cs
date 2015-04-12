using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    [DataContract]
    public class Planet
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public string Blurb { get; set; }
        [DataMember]
        public string Image { get; set; }
        [DataMember]
        public int MissionID { get; set; }
        [DataMember]
        public bool HideIfLocked { get; set; }
        [DataMember]
        public List<String> LinkedPlanets { get; set; }

        public Planet(string id)
        {
            ID = id;
            LinkedPlanets = new List<String>();
            MissionID = -1;
        }
    }
}
