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
    // AccountCampaignProgress
    public partial class AccountCampaignProgress
    {
        public int AccountID { get; set; } // AccountID (Primary key)
        public int CampaignID { get; set; } // CampaignID (Primary key)
        public int PlanetID { get; set; } // PlanetID (Primary key)
        public bool IsUnlocked { get; set; } // IsUnlocked
        public bool IsCompleted { get; set; } // IsCompleted

        // Foreign keys
        public virtual Account Account { get; set; } // FK_AccountCampaignProgress_Account
        public virtual CampaignPlanet CampaignPlanet { get; set; } // FK_AccountCampaignProgress_CampaignPlanet
    }

}
