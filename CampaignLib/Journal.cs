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
        public string Image { get; set; }
        public string Category { get; set; }
        public string PlanetID { get; set; }
        public bool StartUnlocked { get; set; }
        public bool UnlockOnPlanetUnlock { get; set; }
        public bool UnlockOnPlanetCompletion { get; set; }
        public List<String> JournalPartIDs { get; set; }

        public Journal(string id)
        {
            ID = id;
            JournalPartIDs = new List<String>();
            StartUnlocked = false;
            UnlockOnPlanetUnlock = false;
            UnlockOnPlanetCompletion = false;
        }

        //public List<Planet> Planets { get; set; }
    }
}
