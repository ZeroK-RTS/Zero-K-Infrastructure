using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    public class CampaignSave
    {
        public string Name { get; set; }
        public string CampaignID { get; set; }
        public Dictionary<string, CampaignVar> CampaignVars { get; set; }
        public Dictionary<string, PlanetProgressData> PlanetProgress { get; set; }
        public Dictionary<string, JournalProgressData> JournalProgress { get; set; }

        public CampaignSave(string name, string id)
        {
            Name = name;
            CampaignID = id;
            CampaignVars = new Dictionary<string,CampaignVar>();
            PlanetProgress = new Dictionary<string, PlanetProgressData>();
            JournalProgress = new Dictionary<string, JournalProgressData>();
        }

        
        public class PlanetProgressData
        {
            public string planetID;
            public bool played = false;
            public bool unlocked = false;
            public bool completed = false;

            public PlanetProgressData(string planetID)
            {
                this.planetID = planetID;
            }
        }

        
        public class JournalProgressData
        {
            public string journalID;
            public bool unlocked = false;
            public bool read = false;
            public string textSnapshot = "";

            public JournalProgressData(string journalID)
            {
                this.journalID = journalID;
            }

            public void SaveTextSnapshot()
            {
                //textSnapshot = journal.GetJournalText();
            }
        }
    }
}
