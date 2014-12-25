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
    // ForumPostEdit
    public partial class ForumPostEdit
    {
        public int ForumPostEditID { get; set; } // ForumPostEditID (Primary key)
        public int ForumPostID { get; set; } // ForumPostID
        public int EditorAccountID { get; set; } // EditorAccountID
        public string OriginalText { get; set; } // OriginalText
        public string NewText { get; set; } // NewText
        public DateTime EditTime { get; set; } // EditTime

        // Foreign keys
        public virtual Account Account { get; set; } // FK_ForumPostEdit_Account
        public virtual ForumPost ForumPost { get; set; } // FK_ForumPostEdit_ForumPost
    }

}
