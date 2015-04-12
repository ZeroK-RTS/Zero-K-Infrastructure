using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    [DataContract]
    public class CampaignProgress
    {
        [DataMember]
        public string CampaignID { get; set; }
        [DataMember]
        public Dictionary<string, CampaignVar> CampaignVars { get; set; }
        [DataMember]
        public Dictionary<string, PlanetProgressData> PlanetProgress { get; set; }
        [DataMember]
        public Dictionary<string, JournalProgressData> JournalProgress { get; set; }

        public CampaignProgress(string id)
        {
            CampaignID = id;
            CampaignVars = new Dictionary<string,CampaignVar>();
            PlanetProgress = new Dictionary<string, PlanetProgressData>();
            JournalProgress = new Dictionary<string, JournalProgressData>();
        }

        [DataContract]
        public class PlanetProgressData
        {
            [DataMember]
            public string planetID;
            [DataMember]
            public bool played = false;
            [DataMember]
            public bool unlocked = false;
            [DataMember]
            public bool completed = false;

            public PlanetProgressData(string planetID)
            {
                this.planetID = planetID;
            }
        }

        [DataContract]
        public class JournalProgressData
        {
            [DataMember]
            public string journalID;
            [DataMember]
            public bool unlocked = false;
            [DataMember]
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
