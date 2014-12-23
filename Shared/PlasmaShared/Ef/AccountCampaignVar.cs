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
    // AccountCampaignVar
    public partial class AccountCampaignVar
    {
        public int AccountID { get; set; } // AccountID (Primary key)
        public int CampaignID { get; set; } // CampaignID (Primary key)
        public int VarID { get; set; } // VarID (Primary key)
        public string Value { get; set; } // Value

        // Foreign keys
        public virtual Account Account { get; set; } // FK_AccountCampaignVar_Account
        public virtual CampaignVar CampaignVar { get; set; } // FK_AccountCampaignVar_CampaignVar
    }

}
