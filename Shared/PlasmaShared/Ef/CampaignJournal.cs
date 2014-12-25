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
    // CampaignJournal
    public partial class CampaignJournal
    {
        public int CampaignID { get; set; } // CampaignID (Primary key)
        public int JournalID { get; set; } // JournalID (Primary key)
        public int? PlanetID { get; set; } // PlanetID
        public bool UnlockOnPlanetUnlock { get; set; } // UnlockOnPlanetUnlock
        public bool UnlockOnPlanetCompletion { get; set; } // UnlockOnPlanetCompletion
        public bool StartsUnlocked { get; set; } // StartsUnlocked
        public string Title { get; set; } // Title
        public string Text { get; set; } // Text
        public string Category { get; set; } // Category

        // Reverse navigation
        public virtual ICollection<AccountCampaignJournalProgress> AccountCampaignJournalProgress { get; set; } // Many to many mapping
        public virtual ICollection<CampaignJournalVar> CampaignJournalVars { get; set; } // Many to many mapping

        // Foreign keys
        public virtual CampaignPlanet Planet { get; set; } // FK_CampaignJournal_CampaignPlanet

        public CampaignJournal()
        {
            AccountCampaignJournalProgress = new List<AccountCampaignJournalProgress>();
            CampaignJournalVars = new List<CampaignJournalVar>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
