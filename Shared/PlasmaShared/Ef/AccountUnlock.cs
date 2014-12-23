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
    // AccountUnlock
    public partial class AccountUnlock
    {
        public int AccountID { get; set; } // AccountID (Primary key)
        public int UnlockID { get; set; } // UnlockID (Primary key)
        public int Count { get; set; } // Count

        // Foreign keys
        public virtual Account Account { get; set; } // FK_AccountUnlock_Account
        public virtual Unlock Unlock { get; set; } // FK_AccountUnlock_Unlock

        public AccountUnlock()
        {
            Count = 1;
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
