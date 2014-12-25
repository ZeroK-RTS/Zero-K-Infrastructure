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
    // AccountRole
    public partial class AccountRole
    {
        public int AccountID { get; set; } // AccountID (Primary key)
        public int RoleTypeID { get; set; } // RoleTypeID (Primary key)
        public DateTime Inauguration { get; set; } // Inauguration
        public int? FactionID { get; set; } // FactionID
        public int? ClanID { get; set; } // ClanID

        // Foreign keys
        public virtual Account AccountByAccountID { get; set; } // FK_AccountRole_Account
        public virtual Clan Clan { get; set; } // FK_AccountRole_Clan
        public virtual Faction Faction { get; set; } // FK_AccountRole_Faction
        public virtual RoleType RoleType { get; set; } // FK_AccountRole_RoleType
    }

}
