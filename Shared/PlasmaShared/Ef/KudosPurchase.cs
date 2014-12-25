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
    // KudosPurchase
    public partial class KudosPurchase
    {
        public int KudosPurchaseID { get; set; } // KudosPurchaseID (Primary key)
        public int AccountID { get; set; } // AccountID
        public int KudosValue { get; set; } // KudosValue
        public DateTime Time { get; set; } // Time
        public int? UnlockID { get; set; } // UnlockID

        // Foreign keys
        public virtual Account Account { get; set; } // FK_KudosChange_Account
        public virtual Unlock Unlock { get; set; } // FK_KudosChange_Unlock
    }

}
