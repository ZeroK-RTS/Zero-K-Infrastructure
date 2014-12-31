namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("CampaignPlanet")]
    public partial class CampaignPlanet
    {
        
        public CampaignPlanet()
        {
            AccountCampaignProgress = new HashSet<AccountCampaignProgress>();
            CampaignEvents = new HashSet<CampaignEvent>();
            CampaignJournals = new HashSet<CampaignJournal>();
            CampaignLinksByPlanetToUnlock = new HashSet<CampaignLink>();
            CampaignLinksByUnlockingPlanet = new HashSet<CampaignLink>();
            CampaignPlanetVars = new HashSet<CampaignPlanetVar>();
        }

        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PlanetID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        public int MissionID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CampaignID { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public bool IsSkirmish { get; set; }

        public string Description { get; set; }

        public string DescriptionStory { get; set; }

        public bool StartsUnlocked { get; set; }

        public bool HideIfLocked { get; set; }

        [StringLength(100)]
        public string DisplayedMap { get; set; }

        
        public virtual ICollection<AccountCampaignProgress> AccountCampaignProgress { get; set; }

        public virtual Campaign Campaign { get; set; }

        
        public virtual ICollection<CampaignEvent> CampaignEvents { get; set; }

        
        public virtual ICollection<CampaignJournal> CampaignJournals { get; set; }

        
        public virtual ICollection<CampaignLink> CampaignLinksByPlanetToUnlock { get; set; }

        
        public virtual ICollection<CampaignLink> CampaignLinksByUnlockingPlanet { get; set; }

        public virtual Mission Mission { get; set; }

        
        public virtual ICollection<CampaignPlanetVar> CampaignPlanetVars { get; set; }
    }
}
