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
    // ForumThreadLastRead
    public partial class ForumThreadLastRead
    {
        public int ForumThreadID { get; set; } // ForumThreadID (Primary key)
        public int AccountID { get; set; } // AccountID (Primary key)
        public DateTime? LastRead { get; set; } // LastRead
        public DateTime? LastPosted { get; set; } // LastPosted

        // Foreign keys
        public virtual Account Account { get; set; } // FK_ForumThreadLastRead_Account
        public virtual ForumThread ForumThread { get; set; } // FK_ForumThreadLastRead_ForumThread
    }

}
