using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ZkData
{
	partial class CampaignJournal
	{
	    public override string ToString() {
	        return Title;
	    }

        public bool IsUnlocked(int accountID)
        {
            if (StartsUnlocked) return true;

            if (Planet.IsCompleted(accountID) && UnlockOnPlanetCompletion) return true;
            if (Planet.IsUnlocked(accountID) && UnlockOnPlanetUnlock) return true;

            var db = new ZkDataContext();
            AccountCampaignJournalProgress progress = db.AccountCampaignJournalProgress.FirstOrDefault(x => x.AccountID == accountID && x.CampaignID == CampaignID && x.JournalID == JournalID);
            return (progress !=null && progress.IsUnlocked);
        }
	}
}