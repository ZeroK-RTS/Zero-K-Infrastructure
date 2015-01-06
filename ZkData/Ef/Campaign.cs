using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class Campaign
    {
        
        public Campaign()
        {
            AccountCampaignJournalProgresses = new HashSet<AccountCampaignJournalProgress>();
            AccountCampaignProgresses = new HashSet<AccountCampaignProgress>();
            AccountCampaignVars = new HashSet<AccountCampaignVar>();
            CampaignEvents = new HashSet<CampaignEvent>();
            CampaignJournals = new HashSet<CampaignJournal>();
            CampaignJournalVars = new HashSet<CampaignJournalVar>();
            CampaignLinks = new HashSet<CampaignLink>();
            CampaignPlanets = new HashSet<CampaignPlanet>();
            CampaignPlanetVars = new HashSet<CampaignPlanetVar>();
            CampaignVars = new HashSet<CampaignVar>();
        }

        public int CampaignID { get; set; }
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        [StringLength(2000)]
        public string Description { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public bool IsDirty { get; set; }
        public bool? IsHidden { get; set; }
        [StringLength(100)]
        public string MapImageName { get; set; }

        public virtual ICollection<AccountCampaignJournalProgress> AccountCampaignJournalProgresses { get; set; }
        public virtual ICollection<AccountCampaignProgress> AccountCampaignProgresses { get; set; }
        public virtual ICollection<AccountCampaignVar> AccountCampaignVars { get; set; }
        public virtual ICollection<CampaignEvent> CampaignEvents { get; set; }
        public virtual ICollection<CampaignJournal> CampaignJournals { get; set; }
        public virtual ICollection<CampaignJournalVar> CampaignJournalVars { get; set; }
        public virtual ICollection<CampaignLink> CampaignLinks { get; set; }
        public virtual ICollection<CampaignPlanet> CampaignPlanets { get; set; }
        public virtual ICollection<CampaignPlanetVar> CampaignPlanetVars { get; set; }
        public virtual ICollection<CampaignVar> CampaignVars { get; set; }
    }
}
