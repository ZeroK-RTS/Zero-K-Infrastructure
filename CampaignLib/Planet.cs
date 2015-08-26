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
        public double X { get; set; }
        public double Y { get; set; }
        public double Size { get; set; }
        public bool StartUnlocked { get; set; }
        public bool HideIfLocked { get; set; }
        public List<String> LinkedPlanets { get; set; }
        public List<Mission> Missions { get; set; }

        public Planet(string id)
        {
            ID = id;
            LinkedPlanets = new List<String>();
            Missions = new List<Mission>();
            StartUnlocked = false;
			HideIfLocked = true;
            X = 500;
            Y = 500;
            Size = 48;
        }
    }
}
