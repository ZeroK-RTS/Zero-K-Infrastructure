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
    // AccountBattleAward
    public partial class AccountBattleAward
    {
        public int AccountID { get; set; } // AccountID (Primary key)
        public int SpringBattleID { get; set; } // SpringBattleID (Primary key)
        public string AwardKey { get; set; } // AwardKey (Primary key)
        public string AwardDescription { get; set; } // AwardDescription
        public double? Value { get; set; } // Value

        // Foreign keys
        public virtual Account Account { get; set; } // FK_AccountBattleAward_Account
        public virtual SpringBattle SpringBattle { get; set; } // FK_AccountBattleAward_SpringBattle
    }

}
