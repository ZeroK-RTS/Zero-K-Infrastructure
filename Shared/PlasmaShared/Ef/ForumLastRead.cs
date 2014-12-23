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
    // ForumLastRead
    public partial class ForumLastRead
    {
        public int AccountID { get; set; } // AccountID (Primary key)
        public int ForumCategoryID { get; set; } // ForumCategoryID (Primary key)
        public DateTime? LastRead { get; set; } // LastRead

        // Foreign keys
        public virtual Account Account { get; set; } // FK_ForumLastRead_Account
        public virtual ForumCategory ForumCategory { get; set; } // FK_ForumLastRead_ForumCategory
    }

}
