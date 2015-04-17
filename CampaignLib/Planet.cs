using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    public class Planet
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Blurb { get; set; }
        public string Image { get; set; }
        public int MissionID { get; set; }
        public bool HideIfLocked { get; set; }
        public List<String> LinkedPlanets { get; set; }

        public Planet(string id)
        {
            ID = id;
            LinkedPlanets = new List<String>();
            MissionID = -1;
			HideIfLocked = true;
        }
    }
}
