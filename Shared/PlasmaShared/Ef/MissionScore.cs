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
    // MissionScore
    public partial class MissionScore
    {
        public int MissionID { get; set; } // MissionID (Primary key)
        public int AccountID { get; set; } // AccountID (Primary key)
        public int Score { get; set; } // Score
        public DateTime Time { get; set; } // Time
        public int MissionRevision { get; set; } // MissionRevision
        public int GameSeconds { get; set; } // GameSeconds

        // Foreign keys
        public virtual Account Account { get; set; } // FK_MissionScore_Account
        public virtual Mission Mission { get; set; } // FK_MissionScore_Mission
    }

}
