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

            if (CampaignPlanet != null)
            {
                if (CampaignPlanet.IsCompleted(accountID) && UnlockOnPlanetCompletion) return true;
                if (CampaignPlanet.IsUnlocked(accountID) && UnlockOnPlanetUnlock) return true;
            }

            var db = new ZkDataContext();
            AccountCampaignJournalProgress progress = db.AccountCampaignJournalProgress.FirstOrDefault(x => x.AccountID == accountID && x.CampaignID == CampaignID && x.JournalID == JournalID);
            return (progress !=null && progress.IsUnlocked);
        }
	}
}