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
    // CampaignVar
    public partial class CampaignVar
    {
        public int CampaignID { get; set; } // CampaignID (Primary key)
        public int VarID { get; set; } // VarID (Primary key)
        public string KeyString { get; set; } // KeyString
        public string Description { get; set; } // Description

        // Reverse navigation
        public virtual ICollection<AccountCampaignVar> AccountCampaignVars { get; set; } // Many to many mapping
        public virtual ICollection<CampaignJournalVar> CampaignJournalVars { get; set; } // Many to many mapping
        public virtual ICollection<CampaignPlanetVar> CampaignPlanetVars { get; set; } // Many to many mapping

        // Foreign keys
        public virtual Campaign Campaign { get; set; } // FK_CampaignVar_Campaign

        public CampaignVar()
        {
            AccountCampaignVars = new List<AccountCampaignVar>();
            CampaignJournalVars = new List<CampaignJournalVar>();
            CampaignPlanetVars = new List<CampaignPlanetVar>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
