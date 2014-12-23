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
    // AutoBanSmurfList
    public partial class AutoBanSmurfList
    {
        public int AccountID { get; set; } // AccountID (Primary key)
        public bool BanLobby { get; set; } // BanLobby
        public bool BanSite { get; set; } // BanSite
        public bool BanIP { get; set; } // BanIP
        public bool BanUserID { get; set; } // BanUserID

        // Foreign keys
        public virtual Account Account { get; set; } // FK_AutoBanSmurfList_Account
    }

}
