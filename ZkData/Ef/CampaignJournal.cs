using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ZkData
{
    public class CampaignJournal
    {
        
        public CampaignJournal()
        {
            AccountCampaignJournalProgress = new HashSet<AccountCampaignJournalProgress>();
            CampaignJournalVars = new HashSet<CampaignJournalVar>();
        }

        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CampaignID { get; set; }
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int JournalID { get; set; }
        public int? PlanetID { get; set; }
        public bool UnlockOnPlanetUnlock { get; set; }
        public bool UnlockOnPlanetCompletion { get; set; }
        public bool StartsUnlocked { get; set; }
        [Required]
        [StringLength(50)]
        public string Title { get; set; }
        [Required]
        [StringLength(5000)]
        public string Text { get; set; }
        [StringLength(1000)]
        public string Category { get; set; }

        public virtual ICollection<AccountCampaignJournalProgress> AccountCampaignJournalProgress { get; set; }
        public virtual Campaign Campaign { get; set; }
        public virtual CampaignPlanet CampaignPlanet { get; set; }
        public virtual ICollection<CampaignJournalVar> CampaignJournalVars { get; set; }



        public override string ToString()
        {
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
            return (progress != null && progress.IsUnlocked);
        }
    }
}
