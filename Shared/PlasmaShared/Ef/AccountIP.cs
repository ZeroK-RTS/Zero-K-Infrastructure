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
    // AccountIP
    public partial class AccountIP
    {
        public int AccountID { get; set; } // AccountID (Primary key)
        public string IP { get; set; } // IP (Primary key)
        public int LoginCount { get; set; } // LoginCount
        public DateTime FirstLogin { get; set; } // FirstLogin
        public DateTime LastLogin { get; set; } // LastLogin

        // Foreign keys
        public virtual Account Account { get; set; } // FK_AccountIP_Account
    }

}
