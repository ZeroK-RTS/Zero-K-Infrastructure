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

namespace PlasmaShared.Ef
{
    // CampaignPlanet
    public partial class CampaignPlanet
    {
        public int PlanetID { get; set; } // PlanetID (Primary key)
        public string Name { get; set; } // Name
        public int MissionID { get; set; } // MissionID
        public int CampaignID { get; set; } // CampaignID (Primary key)
        public double X { get; set; } // X
        public double Y { get; set; } // Y
        public bool IsSkirmish { get; set; } // IsSkirmish
        public string Description { get; set; } // Description
        public string DescriptionStory { get; set; } // DescriptionStory
        public bool StartsUnlocked { get; set; } // StartsUnlocked
        public bool HideIfLocked { get; set; } // HideIfLocked
        public string DisplayedMap { get; set; } // DisplayedMap

        // Reverse navigation
        public virtual ICollection<AccountCampaignProgress> AccountCampaignProgresses { get; set; } // Many to many mapping
        public virtual ICollection<CampaignEvent> CampaignEvents { get; set; } // Many to many mapping
        public virtual ICollection<CampaignJournal> CampaignJournals { get; set; } // Many to many mapping
        public virtual ICollection<CampaignLink> CampaignLinks_CampaignID { get; set; } // Many to many mapping
        public virtual ICollection<CampaignLink> CampaignLinks1 { get; set; } // Many to many mapping
        public virtual ICollection<CampaignPlanetVar> CampaignPlanetVars { get; set; } // Many to many mapping

        // Foreign keys
        public virtual Campaign Campaign { get; set; } // FK_CampaignPlanet_Campaign
        public virtual Mission Mission { get; set; } // FK_CampaignPlanet_Mission

        public CampaignPlanet()
        {
            AccountCampaignProgresses = new List<AccountCampaignProgress>();
            CampaignEvents = new List<CampaignEvent>();
            CampaignJournals = new List<CampaignJournal>();
            CampaignLinks_CampaignID = new List<CampaignLink>();
            CampaignLinks1 = new List<CampaignLink>();
            CampaignPlanetVars = new List<CampaignPlanetVar>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
