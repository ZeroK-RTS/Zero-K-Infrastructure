using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    public class Mission
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string IntroJournal { get; set; }
        public string DownloadArchive { get; set; }
        public int SiteNum { get; set; }
        public bool IsMainQuest { get; set; }
        public string Archive { get; set; }
        public string Image { get; set; }
        public bool UnlockOnPreviousPlanetCompletion { get; set; }
        public bool StartUnlocked { get; set; }
        public bool HideIfLocked { get; set; }

        public Mission(string id)
        {
            ID = id;
            StartUnlocked = false;
			HideIfLocked = true;
            IsMainQuest = true;
            UnlockOnPreviousPlanetCompletion = true;
        }
    }
}
