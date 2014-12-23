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
    // AccountCampaignJournalProgress
    public partial class AccountCampaignJournalProgress
    {
        public int AccountID { get; set; } // AccountID (Primary key)
        public int CampaignID { get; set; } // CampaignID (Primary key)
        public int JournalID { get; set; } // JournalID (Primary key)
        public bool IsUnlocked { get; set; } // IsUnlocked

        // Foreign keys
        public virtual Account Account { get; set; } // FK_AccountCampaignJournalProgress_Account
        public virtual CampaignJournal CampaignJournal { get; set; } // FK_AccountCampaignJournalProgress_CampaignJournal
    }

}
