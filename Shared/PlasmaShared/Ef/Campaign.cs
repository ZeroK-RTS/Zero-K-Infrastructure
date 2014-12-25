// ReSharper disable RedundantUsingDirective
// ReSharper disable DoNotCallOverridableMethodsInConstructor
// ReSharper disable InconsistentNaming
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable RedundantNameQualifier

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
//using DatabaseGeneratedOption = System.ComponentModel.DataAnnotations.DatabaseGeneratedOption;

namespace ZkData
{
    // Campaign
    public partial class Campaign
    {
        public int CampaignID { get; set; } // CampaignID (Primary key)
        public string Name { get; set; } // Name
        public string Description { get; set; } // Description
        public int MapWidth { get; set; } // MapWidth
        public int MapHeight { get; set; } // MapHeight
        public bool IsDirty { get; set; } // IsDirty
        public bool? IsHidden { get; set; } // IsHidden
        public string MapImageName { get; set; } // MapImageName

        // Reverse navigation
        public virtual ICollection<AccountCampaignJournalProgress> AccountCampaignJournalProgresses { get; set; } // Many to many mapping
        public virtual ICollection<AccountCampaignProgress> AccountCampaignProgresses { get; set; } // Many to many mapping
        public virtual ICollection<AccountCampaignVar> AccountCampaignVars { get; set; } // Many to many mapping
        public virtual ICollection<CampaignEvent> CampaignEvents { get; set; } // CampaignEvent.FK_CampaignEvent_Campaign
        public virtual ICollection<CampaignJournal> CampaignJournals { get; set; } // Many to many mapping
        public virtual ICollection<CampaignJournalVar> CampaignJournalVars { get; set; } // Many to many mapping
        public virtual ICollection<CampaignLink> CampaignLinks { get; set; } // CampaignLink.FK_CampaignLink_Campaign
        public virtual ICollection<CampaignPlanet> CampaignPlanets { get; set; } // Many to many mapping
        public virtual ICollection<CampaignPlanetVar> CampaignPlanetVars { get; set; } // Many to many mapping
        public virtual ICollection<CampaignVar> CampaignVars { get; set; } // Many to many mapping

        public Campaign()
        {
            AccountCampaignJournalProgresses = new List<AccountCampaignJournalProgress>();
            AccountCampaignProgresses = new List<AccountCampaignProgress>();
            AccountCampaignVars = new List<AccountCampaignVar>();
            CampaignEvents = new List<CampaignEvent>();
            CampaignJournals = new List<CampaignJournal>();
            CampaignJournalVars = new List<CampaignJournalVar>();
            CampaignLinks = new List<CampaignLink>();
            CampaignPlanets = new List<CampaignPlanet>();
            CampaignPlanetVars = new List<CampaignPlanetVar>();
            CampaignVars = new List<CampaignVar>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
